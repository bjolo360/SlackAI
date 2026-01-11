from __future__ import annotations

from dataclasses import dataclass, field
from datetime import datetime
from typing import Iterable, Sequence


@dataclass(frozen=True)
class AuthorizationContext:
    workspace_id: str
    user_id: str
    allowed_list_ids: Sequence[str] = field(default_factory=list)


@dataclass(frozen=True)
class ListItem:
    id: str
    list_id: str
    workspace_id: str
    content: str
    created_at: datetime


@dataclass(frozen=True)
class ListReference:
    item_id: str
    list_id: str
    snippet: str


@dataclass(frozen=True)
class ListQueryIntent:
    query: str
    keywords: Sequence[str]
    list_ids: Sequence[str] = field(default_factory=list)
    limit: int = 5


@dataclass(frozen=True)
class Answer:
    text: str
    references: Iterable[ListReference]
