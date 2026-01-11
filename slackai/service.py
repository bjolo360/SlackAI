from __future__ import annotations

from dataclasses import dataclass
from typing import Iterable

from .llm import QueryInterpreter
from .models import Answer, AuthorizationContext, ListReference
from .retrieval import RetrievalLayer


@dataclass
class Guardrails:
    def enforce_workspace(self, auth: AuthorizationContext) -> None:
        if not auth.workspace_id:
            raise ValueError("workspace_id is required")

    def filter_question(self, question: str) -> str:
        return question.strip()


class ListQuestionAnswerer:
    def __init__(
        self,
        retrieval: RetrievalLayer,
        interpreter: QueryInterpreter,
        guardrails: Guardrails | None = None,
    ) -> None:
        self._retrieval = retrieval
        self._interpreter = interpreter
        self._guardrails = guardrails or Guardrails()

    def answer(self, question: str, auth: AuthorizationContext) -> Answer:
        self._guardrails.enforce_workspace(auth)
        sanitized = self._guardrails.filter_question(question)
        intent = self._interpreter.interpret(sanitized)
        filtered_intent = self._retrieval.filter_allowed_lists(
            intent, auth.allowed_list_ids
        )
        if intent.list_ids and not filtered_intent.list_ids:
            return Answer(
                text=\"I can only answer questions within your authorized lists.\",
                references=[],
            )
        intent = filtered_intent
        results = self._retrieval.retrieve(auth.workspace_id, intent)
        if not results.items:
            return Answer(
                text="I couldn't find matching list items in this workspace.",
                references=[],
            )
        references = list(self._build_references(results.items))
        answer_text = self._summarize(references)
        return Answer(text=answer_text, references=references)

    def _build_references(self, items) -> Iterable[ListReference]:
        for item in items:
            snippet = item.content
            if len(snippet) > 140:
                snippet = f"{snippet[:137]}..."
            yield ListReference(
                item_id=item.id,
                list_id=item.list_id,
                snippet=snippet,
            )

    def _summarize(self, references: list[ListReference]) -> str:
        top = references[:3]
        summaries = ", ".join(ref.snippet for ref in top)
        return f"Top matches: {summaries}"
