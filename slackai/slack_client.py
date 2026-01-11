from __future__ import annotations

from dataclasses import dataclass
from typing import Iterable, List

import requests


@dataclass
class SlackApiError(RuntimeError):
    message: str


class SlackClient:
    def __init__(self, token: str, base_url: str = "https://slack.com/api") -> None:
        self._token = token
        self._base_url = base_url.rstrip("/")
        self._session = requests.Session()
        self._session.headers.update({"Authorization": f"Bearer {self._token}"})

    def list_channels(self) -> List[str]:
        response = self._session.get(f"{self._base_url}/conversations.list")
        payload = response.json()
        if not payload.get("ok"):
            raise SlackApiError(payload.get("error", "unknown_error"))
        channels = payload.get("channels", [])
        return [channel["name"] for channel in channels if "name" in channel]

    def create_channel(self, name: str) -> None:
        response = self._session.post(
            f"{self._base_url}/conversations.create",
            json={"name": name},
        )
        payload = response.json()
        if not payload.get("ok"):
            raise SlackApiError(payload.get("error", "unknown_error"))

    def ensure_channels(self, desired_channels: Iterable[str]) -> List[str]:
        existing = set(self.list_channels())
        created = []
        for channel in desired_channels:
            if channel not in existing:
                self.create_channel(channel)
                created.append(channel)
        return created
