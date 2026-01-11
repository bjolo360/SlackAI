from __future__ import annotations

from dataclasses import dataclass
from typing import Iterable, Sequence

from .models import ListItem, ListQueryIntent
from .storage import Storage


@dataclass
class RetrievalResult:
    items: Sequence[ListItem]


class RetrievalLayer:
    def __init__(self, storage: Storage) -> None:
        self._storage = storage

    def retrieve(
        self, workspace_id: str, intent: ListQueryIntent
    ) -> RetrievalResult:
        query_terms = intent.query.strip()
        if not query_terms:
            query_terms = " OR ".join(intent.keywords)
        items = list(
            self._storage.search_items(
                workspace_id=workspace_id,
                query=query_terms,
                list_ids=intent.list_ids,
                limit=intent.limit,
            )
        )
        return RetrievalResult(items=items)

    def filter_allowed_lists(
        self, intent: ListQueryIntent, allowed_list_ids: Sequence[str]
    ) -> ListQueryIntent:
        if not intent.list_ids:
            return intent
        filtered = [list_id for list_id in intent.list_ids if list_id in allowed_list_ids]
        return ListQueryIntent(
            query=intent.query,
            keywords=intent.keywords,
            list_ids=filtered,
            limit=intent.limit,
        )

    @staticmethod
    def extract_keywords(question: str) -> Iterable[str]:
        return [
            token.strip(".,?!")
            for token in question.split()
            if len(token.strip(".,?!")) > 3
        ]
