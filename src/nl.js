const { filterListsForUser } = require("./permissions");

const SAMPLE_LIST_ITEMS = [
  {
    listId: "list_123",
    itemId: "item_a",
    title: "Draft onboarding checklist",
  },
  {
    listId: "list_456",
    itemId: "item_b",
    title: "Review Q3 roadmap",
  },
];

function handleNlQuery({ query, userId }) {
  const trimmed = query.trim();
  const results = SAMPLE_LIST_ITEMS.filter((item) =>
    item.title.toLowerCase().includes(trimmed.toLowerCase())
  );

  return {
    results: filterListsForUser(userId, results),
  };
}

module.exports = {
  handleNlQuery,
};
