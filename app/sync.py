import logging
from datetime import datetime, timezone

from sqlalchemy import select

from app.db import get_sessionmaker
from app.models import List, ListItem, SyncRun
from app.slack_client import SlackAPIClient

logger = logging.getLogger(__name__)


async def sync_lists() -> None:
    sessionmaker = get_sessionmaker()
    async with sessionmaker() as session:
        sync_run = SyncRun(started_at=datetime.now(tz=timezone.utc), status="running", details=None)
        session.add(sync_run)
        await session.commit()
        await session.refresh(sync_run)

        client = SlackAPIClient()
        try:
            lists = await client.fetch_lists()
            for list_payload in lists:
                slack_id = list_payload.get("id")
                if not slack_id:
                    continue
                existing = await session.scalar(select(List).where(List.slack_id == slack_id))
                if existing is None:
                    existing = List(
                        slack_id=slack_id,
                        name=list_payload.get("name", ""),
                        description=list_payload.get("description"),
                        is_archived=bool(list_payload.get("is_archived", False)),
                        updated_at=client.parse_timestamp(list_payload.get("updated_ts")),
                    )
                    session.add(existing)
                else:
                    existing.name = list_payload.get("name", existing.name)
                    existing.description = list_payload.get("description")
                    existing.is_archived = bool(list_payload.get("is_archived", existing.is_archived))
                    existing.updated_at = client.parse_timestamp(list_payload.get("updated_ts"))

                items = await client.fetch_list_items(slack_id)
                for item_payload in items:
                    item_id = item_payload.get("id")
                    if not item_id:
                        continue
                    item = await session.scalar(select(ListItem).where(ListItem.slack_id == item_id))
                    if item is None:
                        item = ListItem(
                            slack_id=item_id,
                            list=existing,
                            title=item_payload.get("title", ""),
                            status=item_payload.get("status"),
                            assignee=item_payload.get("assignee"),
                            updated_at=client.parse_timestamp(item_payload.get("updated_ts")),
                        )
                        session.add(item)
                    else:
                        item.title = item_payload.get("title", item.title)
                        item.status = item_payload.get("status")
                        item.assignee = item_payload.get("assignee")
                        item.updated_at = client.parse_timestamp(item_payload.get("updated_ts"))

            sync_run.status = "success"
            sync_run.finished_at = datetime.now(tz=timezone.utc)
            await session.commit()
            logger.info("Slack list sync completed")
        except Exception as exc:  # noqa: BLE001
            sync_run.status = "failed"
            sync_run.details = str(exc)
            sync_run.finished_at = datetime.now(tz=timezone.utc)
            await session.commit()
            logger.exception("Slack list sync failed")
            raise
        finally:
            await client.close()
