# SlackAI

Slack bot + NL query service for Slack Lists.

## Setup

1. Install dependencies: `npm install`
2. Provide env vars:
   - `SLACK_SIGNING_SECRET`
   - `SLACK_BOT_TOKEN`
   - `ALLOWED_SLACK_USER_IDS` (comma-separated list, optional)
   - `LIST_ACCESS_BY_USER` (JSON map of userId -> listId array)
   - `NL_QUERY_URL` (optional, defaults to local `/api/nl-query`)
3. Run the server: `npm start`

## Slack configuration

- Events endpoint: `/slack/events`
- Slash command: `/slack/commands` with command `/list-ask`

The server routes incoming Slack messages to the NL endpoint, filters results based
on per-user access, and formats responses with Slack List links.
