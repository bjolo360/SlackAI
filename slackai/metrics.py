from __future__ import annotations

import time
from contextlib import contextmanager
from typing import Iterator

from prometheus_client import Counter, Gauge, Histogram

SYNC_RUNS = Counter("slackai_sync_runs_total", "Total number of sync runs")
SYNC_ERRORS = Counter("slackai_sync_errors_total", "Total number of sync errors")
SYNC_LATENCY = Histogram(
    "slackai_sync_latency_seconds",
    "Latency for sync runs in seconds",
    buckets=(0.5, 1, 2, 5, 10, 30, 60),
)
SYNC_LAST_SUCCESS = Gauge(
    "slackai_sync_last_success_timestamp",
    "Unix timestamp of the last successful sync",
)


@contextmanager
def track_sync() -> Iterator[None]:
    start = time.monotonic()
    SYNC_RUNS.inc()
    try:
        yield
    except Exception:  # pragma: no cover - re-raised after metric update
        SYNC_ERRORS.inc()
        raise
    else:
        SYNC_LAST_SUCCESS.set(time.time())
    finally:
        SYNC_LATENCY.observe(time.monotonic() - start)
