import responses

from slackai.slack_client import SlackClient


@responses.activate
def test_list_channels_calls_slack_api():
    responses.add(
        responses.GET,
        "https://example.com/api/conversations.list",
        json={"ok": True, "channels": [{"name": "general"}, {"name": "random"}]},
        status=200,
    )

    client = SlackClient("token", base_url="https://example.com/api")

    assert client.list_channels() == ["general", "random"]


@responses.activate
def test_create_channel_calls_slack_api():
    responses.add(
        responses.POST,
        "https://example.com/api/conversations.create",
        json={"ok": True},
        status=200,
    )

    client = SlackClient("token", base_url="https://example.com/api")

    client.create_channel("alerts")

    assert len(responses.calls) == 1
