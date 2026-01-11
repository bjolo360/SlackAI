# SlackAI

Slack List sync service built on **Python + FastAPI**. It can run scheduled syncs or be triggered via Slack events.

## Setup

1. Create a virtual environment and install dependencies:

```bash
python -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
```

2. Configure environment variables (defaults shown):

```bash
export SLACKAI_DATABASE_URL="postgresql+asyncpg://postgres:postgres@localhost:5432/slackai"
export SLACKAI_SLACK_TOKEN="xoxb-your-token"
export SLACKAI_SYNC_INTERVAL_SECONDS=300
```

3. Run the API:

```bash
uvicorn app.main:app --reload
```

## Endpoints

- `POST /slack/events/lists` — Slack event webhook to trigger a sync.
- `GET /health` — health check.

## Data model

The service stores normalized list metadata and items in Postgres for fast querying.

## Notes

- The scheduler runs every `SLACKAI_SYNC_INTERVAL_SECONDS` seconds.
- Sync status is logged and stored in the `sync_runs` table.
