# Slack Lists CLI (C#)

A command-line tool built in C# that connects to your Slack workspace and answers natural-language questions about Slack Lists, such as:

- "How many unresolved items are there in the list Product Launch"
- "What is person Y working on"

The CLI uses Slack Web API calls and a lightweight natural-language router to map questions to list and user lookups.

## Features

- Natural-language questions for common list queries.
- Connects to your Slack workspace using a bot token.
- Filters unresolved list items.
- Shows what a person is working on across all lists.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download) installed locally.
- A Slack app installed in your workspace with a bot token.
- Slack Lists enabled for your workspace.

## Slack App Setup

1. Create a Slack app in your workspace.
2. Add a bot token scope with the following permissions:
   - `lists:read` (read Slack Lists)
   - `users:read` (resolve names to user IDs)
   - `users:read.email` (optional, enables lookup by email)
3. Install the app to your workspace.
4. Copy the bot token (`xoxb-...`).

## Configuration

Set the Slack bot token in your environment:

```bash
export SLACK_BOT_TOKEN="xoxb-your-token"
```

Optional: provide the workspace domain if you want to include it in future extensions:

```bash
export SLACK_WORKSPACE_DOMAIN="your-workspace"
```

## Build

```bash
dotnet build SlackListsCli/SlackListsCli.csproj
```

## Run

```bash
dotnet run --project SlackListsCli/SlackListsCli.csproj -- \
  --question "How many unresolved items are there in the list Product Launch"
```

```bash
dotnet run --project SlackListsCli/SlackListsCli.csproj -- \
  --question "What is person Ada Lovelace working on"
```

## Example Output

```
There are 4 unresolved items in 'Product Launch'.
```

```
Ada Lovelace is working on:
- Draft launch checklist (List: Product Launch, Status: in_progress)
- Verify tracking plan (List: Analytics, Status: open)
```

## How It Works

The CLI parses questions using regular expressions and maps them to Slack API calls:

- `lists.list` to resolve list names.
- `lists.items` to fetch list items and count unresolved items.
- `users.list` to resolve user names.

For "what is person X working on", the tool:

1. Looks up the user ID.
2. Loads all lists.
3. Pulls items from each list and filters by assignee.

## Extending the Natural Language Router

The intent handling lives in `SlackListsCli/Services/NaturalLanguageRouter.cs`. Add new regex patterns and handlers to support more questions.

## Troubleshooting

- **Invalid token**: Ensure `SLACK_BOT_TOKEN` is set and the app is installed.
- **Missing scopes**: Add the required scopes and reinstall the app.
- **List not found**: Ensure the list name matches exactly (case-insensitive).

## License

MIT
