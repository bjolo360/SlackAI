import asyncio
import logging
from datetime import datetime, timezone
from typing import Any

import httpx

from app.settings import settings

logger = logging.getLogger(__name__)


class SlackAPIClient:
    def __init__(self) -> None:
        self._client = httpx.AsyncClient(
            base_url=settings.slack_api_base,
            headers={"Authorization": f"Bearer {settings.slack_token}"},
            timeout=settings.request_timeout_seconds,
        )

    async def close(self) -> None:
        await self._client.aclose()

    async def _request(self, method: str, path: str, params: dict[str, Any] | None = None) -> dict[str, Any]:
        retries = 0
        while True:
            response = await self._client.request(method, path, params=params)
            if response.status_code == 429:
                retry_after = int(response.headers.get("Retry-After", "1"))
                sleep_for = min(retry_after * (2**retries), 60)
                logger.warning("Slack rate limit hit, backing off for %s seconds", sleep_for)
                await asyncio.sleep(sleep_for)
                retries += 1
            elif response.status_code >= 500 and retries < settings.max_retries:
                sleep_for = min(2**retries, 60)
                logger.warning("Slack server error %s, retrying in %s seconds", response.status_code, sleep_for)
                await asyncio.sleep(sleep_for)
                retries += 1
            else:
                response.raise_for_status()
                payload = response.json()
                if not payload.get("ok", False):
                    error = payload.get("error", "unknown_error")
                    raise RuntimeError(f"Slack API error: {error}")
                return payload

            if retries >= settings.max_retries:
                raise RuntimeError("Exceeded Slack API retry limit")

    async def fetch_lists(self) -> list[dict[str, Any]]:
        payload = await self._request("GET", "/lists.list")
        return payload.get("lists", [])

    async def fetch_list_items(self, list_id: str) -> list[dict[str, Any]]:
        payload = await self._request("GET", "/lists.items", params={"list_id": list_id})
        return payload.get("items", [])

    @staticmethod
    def parse_timestamp(raw: str | None) -> datetime:
        if not raw:
            return datetime.now(tz=timezone.utc)
        return datetime.fromtimestamp(float(raw), tz=timezone.utc)
