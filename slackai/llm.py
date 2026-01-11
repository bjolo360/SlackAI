from __future__ import annotations

import json
from dataclasses import dataclass
from typing import Protocol

from .models import ListQueryIntent
from .retrieval import RetrievalLayer


class LLMClient(Protocol):
    def complete(self, prompt: str) -> str:
        ...


@dataclass
class QueryInterpreter:
    llm_client: LLMClient | None = None

    def interpret(self, question: str) -> ListQueryIntent:
        if self.llm_client is None:
            return self._fallback_intent(question)
        prompt = (
            "Convert the user question into JSON for a list search. "
            "Return JSON with keys: query, keywords, list_ids, limit. "
            "Use list_ids only if the question names a specific list id. "
            f"Question: {question}"
        )
        raw = self.llm_client.complete(prompt)
        try:
            payload = json.loads(raw)
        except json.JSONDecodeError:
            return self._fallback_intent(question)
        return ListQueryIntent(
            query=str(payload.get("query", "")),
            keywords=list(payload.get("keywords", [])),
            list_ids=list(payload.get("list_ids", [])),
            limit=int(payload.get("limit", 5)),
        )

    def _fallback_intent(self, question: str) -> ListQueryIntent:
        keywords = list(RetrievalLayer.extract_keywords(question))
        return ListQueryIntent(query=question, keywords=keywords, list_ids=[], limit=5)
