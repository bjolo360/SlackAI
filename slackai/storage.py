from __future__ import annotations

import sqlite3
from dataclasses import dataclass
from datetime import datetime
from typing import Iterable, Sequence

from .models import ListItem


@dataclass
class ListRecord:
    id: str
    workspace_id: str
    title: str


class Storage:
    def __init__(self, db_path: str) -> None:
        self._db_path = db_path
        self._init_db()

    def _connect(self) -> sqlite3.Connection:
        connection = sqlite3.connect(self._db_path)
        connection.row_factory = sqlite3.Row
        return connection

    def _init_db(self) -> None:
        with self._connect() as connection:
            connection.execute(
                """
                CREATE TABLE IF NOT EXISTS lists (
                    id TEXT PRIMARY KEY,
                    workspace_id TEXT NOT NULL,
                    title TEXT NOT NULL
                )
                """
            )
            connection.execute(
                """
                CREATE TABLE IF NOT EXISTS list_items (
                    id TEXT PRIMARY KEY,
                    list_id TEXT NOT NULL,
                    workspace_id TEXT NOT NULL,
                    content TEXT NOT NULL,
                    created_at TEXT NOT NULL,
                    FOREIGN KEY(list_id) REFERENCES lists(id)
                )
                """
            )
            connection.execute(
                """
                CREATE VIRTUAL TABLE IF NOT EXISTS list_items_fts
                USING fts5(item_id, list_id, workspace_id, content)
                """
            )

    def add_list(self, record: ListRecord) -> None:
        with self._connect() as connection:
            connection.execute(
                "INSERT OR REPLACE INTO lists (id, workspace_id, title) VALUES (?, ?, ?)",
                (record.id, record.workspace_id, record.title),
            )

    def add_item(self, item: ListItem) -> None:
        with self._connect() as connection:
            connection.execute(
                """
                INSERT OR REPLACE INTO list_items
                (id, list_id, workspace_id, content, created_at)
                VALUES (?, ?, ?, ?, ?)
                """,
                (
                    item.id,
                    item.list_id,
                    item.workspace_id,
                    item.content,
                    item.created_at.isoformat(),
                ),
            )
            connection.execute(
                """
                INSERT INTO list_items_fts (item_id, list_id, workspace_id, content)
                VALUES (?, ?, ?, ?)
                """,
                (item.id, item.list_id, item.workspace_id, item.content),
            )

    def search_items(
        self,
        workspace_id: str,
        query: str,
        list_ids: Sequence[str],
        limit: int,
    ) -> Iterable[ListItem]:
        list_filter = ""
        params: list[object] = [workspace_id, query]
        if list_ids:
            placeholders = ",".join("?" for _ in list_ids)
            list_filter = f"AND list_id IN ({placeholders})"
            params.extend(list_ids)
        params.append(limit)
        sql = (
            "SELECT item_id, list_id, workspace_id, content "
            "FROM list_items_fts "
            "WHERE workspace_id = ? AND list_items_fts MATCH ? "
            f"{list_filter} "
            "LIMIT ?"
        )
        with self._connect() as connection:
            rows = connection.execute(sql, params).fetchall()
        return [
            ListItem(
                id=row["item_id"],
                list_id=row["list_id"],
                workspace_id=row["workspace_id"],
                content=row["content"],
                created_at=datetime.utcnow(),
            )
            for row in rows
        ]

    def load_items(self, item_ids: Sequence[str]) -> Iterable[ListItem]:
        if not item_ids:
            return []
        placeholders = ",".join("?" for _ in item_ids)
        sql = (
            "SELECT id, list_id, workspace_id, content, created_at "
            "FROM list_items "
            f"WHERE id IN ({placeholders})"
        )
        with self._connect() as connection:
            rows = connection.execute(sql, item_ids).fetchall()
        return [
            ListItem(
                id=row["id"],
                list_id=row["list_id"],
                workspace_id=row["workspace_id"],
                content=row["content"],
                created_at=datetime.fromisoformat(row["created_at"]),
            )
            for row in rows
        ]
