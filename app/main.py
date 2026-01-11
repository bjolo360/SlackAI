import logging

from apscheduler.schedulers.asyncio import AsyncIOScheduler
from fastapi import FastAPI, HTTPException, Request

from app.db import Base, get_engine
from app.settings import settings
from app.sync import sync_lists

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

app = FastAPI(title="SlackAI List Sync")

_scheduler: AsyncIOScheduler | None = None


@app.on_event("startup")
async def startup() -> None:
    engine = get_engine()
    async with engine.begin() as conn:
        await conn.run_sync(Base.metadata.create_all)

    global _scheduler
    _scheduler = AsyncIOScheduler()
    _scheduler.add_job(sync_lists, "interval", seconds=settings.sync_interval_seconds)
    _scheduler.start()
    logger.info("Scheduler started with interval %s", settings.sync_interval_seconds)


@app.on_event("shutdown")
async def shutdown() -> None:
    if _scheduler:
        _scheduler.shutdown()


@app.post("/slack/events/lists")
async def handle_lists_event(request: Request) -> dict[str, str]:
    payload = await request.json()
    if payload.get("type") == "url_verification":
        return {"challenge": payload.get("challenge", "")}

    event_type = payload.get("event", {}).get("type")
    if not event_type:
        raise HTTPException(status_code=400, detail="Missing event type")

    await sync_lists()
    return {"status": "sync_started"}


@app.get("/health")
async def health() -> dict[str, str]:
    return {"status": "ok"}
