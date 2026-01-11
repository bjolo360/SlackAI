from __future__ import annotations

import logging
import time
from typing import Iterable

from prometheus_client import start_http_server

from slackai.config import Config, load_config
from slackai.slack_client import SlackClient
from slackai.sync import sync_channels


logger = logging.getLogger(__name__)


def run_sync_once(config: Config) -> None:
    client = SlackClient(config.slack_token, config.slack_api_base_url)
    sync_channels(client, config.desired_channels)


def run_sync_loop(config: Config) -> None:
    logger.info("starting sync loop", extra={"interval_seconds": config.sync_interval_seconds})
    while True:
        run_sync_once(config)
        time.sleep(config.sync_interval_seconds)


def main(desired_channels: Iterable[str] | None = None) -> None:
    config = load_config()
    if desired_channels is not None:
        config = Config(
            slack_token=config.slack_token,
            slack_api_base_url=config.slack_api_base_url,
            sync_interval_seconds=config.sync_interval_seconds,
            log_level=config.log_level,
            desired_channels=list(desired_channels),
        )

    logging.basicConfig(level=config.log_level)
    start_http_server(8000)
    run_sync_loop(config)


if __name__ == "__main__":
    main()
