function parseAllowedUserIds() {
  return (process.env.ALLOWED_SLACK_USER_IDS || "")
    .split(",")
    .map((id) => id.trim())
    .filter(Boolean);
}

function parseListAccessMap() {
  if (!process.env.LIST_ACCESS_BY_USER) {
    return {};
  }

  try {
    return JSON.parse(process.env.LIST_ACCESS_BY_USER);
  } catch (error) {
    console.warn("Invalid LIST_ACCESS_BY_USER JSON. Falling back to empty map.");
    return {};
  }
}

function isUserAllowed(userId) {
  const allowed = parseAllowedUserIds();
  if (allowed.length === 0) {
    return true;
  }

  return allowed.includes(userId);
}

function filterListsForUser(userId, listItems) {
  const accessMap = parseListAccessMap();
  const allowedLists = accessMap[userId];

  if (!allowedLists || allowedLists.length === 0) {
    return [];
  }

  return listItems.filter((item) => allowedLists.includes(item.listId));
}

module.exports = {
  filterListsForUser,
  isUserAllowed,
};
