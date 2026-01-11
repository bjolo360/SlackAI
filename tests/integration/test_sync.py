import responses

from slackai.slack_client import SlackClient
from slackai.sync import sync_channels


@responses.activate
def test_sync_channels_creates_missing_channels():
    responses.add(
        responses.GET,
        "https://example.com/api/conversations.list",
        json={"ok": True, "channels": [{"name": "general"}]},
        status=200,
    )
    responses.add(
        responses.POST,
        "https://example.com/api/conversations.create",
        json={"ok": True},
        status=200,
    )

    client = SlackClient("token", base_url="https://example.com/api")

    created = sync_channels(client, ["general", "alerts"])

    assert created == ["alerts"]
