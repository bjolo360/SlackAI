from __future__ import annotations

import os
from dataclasses import dataclass
from typing import List


@dataclass(frozen=True)
class Config:
    slack_token: str
    slack_api_base_url: str
    sync_interval_seconds: int
    log_level: str
    desired_channels: List[str]



def _parse_desired_channels(value: str) -> List[str]:
    return [channel.strip() for channel in value.split(",") if channel.strip()]



def load_config() -> Config:
    slack_token = os.environ.get("SLACK_BOT_TOKEN", "").strip()
    if not slack_token:
        raise ValueError("SLACK_BOT_TOKEN is required")

    base_url = os.environ.get("SLACK_API_BASE_URL", "https://slack.com/api").strip()
    sync_interval = int(os.environ.get("SYNC_INTERVAL_SECONDS", "300"))
    log_level = os.environ.get("LOG_LEVEL", "INFO").strip()
    desired_channels = _parse_desired_channels(os.environ.get("DESIRED_CHANNELS", ""))

    if not desired_channels:
        raise ValueError("DESIRED_CHANNELS must include at least one channel")

    return Config(
        slack_token=slack_token,
        slack_api_base_url=base_url,
        sync_interval_seconds=sync_interval,
        log_level=log_level,
        desired_channels=desired_channels,
    )
