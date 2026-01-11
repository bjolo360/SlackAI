# SlackAI

SlackAI keeps your Slack workspace aligned with an approved list of channels by periodically
creating missing channels and exposing metrics for sync health.

## Setup

### 1. Create and install the Slack app

1. Create a new Slack app in your workspace.
2. Add a bot user.
3. Add the following bot token scopes:
   - `channels:read` (read public channels)
   - `channels:manage` (create public channels)
4. Install the app to your workspace and copy the bot token.

### 2. Configure environment variables

| Variable | Description |
| --- | --- |
| `SLACK_BOT_TOKEN` | Slack bot token with required scopes. |
| `DESIRED_CHANNELS` | Comma-separated channel names to enforce (e.g. `alerts,incidents`). |
| `SYNC_INTERVAL_SECONDS` | Sync interval in seconds (default: `300`). |
| `LOG_LEVEL` | Log level (default: `INFO`). |
| `SLACK_API_BASE_URL` | Optional Slack API base URL for testing. |

### 3. Install dependencies

```bash
pip install -r requirements.txt
```

### 4. Run the sync service

```bash
export SLACK_BOT_TOKEN="xoxb-..."
export DESIRED_CHANNELS="alerts,incidents"
python -m slackai.service
```

The service exposes Prometheus metrics on port `8000`.

## Deployment

A Dockerfile and Kubernetes manifests are available in `deploy/k8s`.

```bash
docker build -t slackai-sync:latest .
```

For Kubernetes, store the Slack token in a secret (or use an external secrets manager)
then apply the manifests:

```bash
kubectl apply -f deploy/k8s/secret.yaml
kubectl apply -f deploy/k8s/deployment.yaml
```

## Usage

Update the `DESIRED_CHANNELS` environment variable when you need to add or remove
channels from the sync list. The service will create any missing channels on the
next sync cycle.

## Testing

```bash
pip install -r requirements-dev.txt
pytest
```
