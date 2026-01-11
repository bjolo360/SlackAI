from __future__ import annotations

import logging
from typing import Iterable, List

from slackai.metrics import track_sync
from slackai.slack_client import SlackClient

logger = logging.getLogger(__name__)


def sync_channels(client: SlackClient, desired_channels: Iterable[str]) -> List[str]:
    desired_list = list(desired_channels)
    logger.info("starting channel sync", extra={"desired_channels": desired_list})

    with track_sync():
        created = client.ensure_channels(desired_list)

    if created:
        logger.info("created missing channels", extra={"created_channels": created})
    else:
        logger.info("all channels already present")

    return created
