const crypto = require("crypto");
const fetch = require("node-fetch");

const SLACK_API_BASE = "https://slack.com/api";

function verifySlackSignature(req) {
  const signingSecret = process.env.SLACK_SIGNING_SECRET;
  if (!signingSecret) {
    return false;
  }

  const timestamp = req.headers["x-slack-request-timestamp"];
  const signature = req.headers["x-slack-signature"];

  if (!timestamp || !signature || !req.rawBody) {
    return false;
  }

  const fiveMinutes = 60 * 5;
  const now = Math.floor(Date.now() / 1000);
  if (Math.abs(now - Number(timestamp)) > fiveMinutes) {
    return false;
  }

  const baseString = `v0:${timestamp}:${req.rawBody}`;
  const hash = crypto
    .createHmac("sha256", signingSecret)
    .update(baseString, "utf8")
    .digest("hex");
  const expected = `v0=${hash}`;

  return crypto.timingSafeEqual(
    Buffer.from(expected, "utf8"),
    Buffer.from(signature, "utf8")
  );
}

async function sendSlackMessage({ channel, text }) {
  const token = process.env.SLACK_BOT_TOKEN;
  if (!token) {
    throw new Error("Missing SLACK_BOT_TOKEN.");
  }

  const response = await fetch(`${SLACK_API_BASE}/chat.postMessage`, {
    method: "POST",
    headers: {
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json; charset=utf-8",
    },
    body: JSON.stringify({ channel, text }),
  });

  const payload = await response.json();
  if (!payload.ok) {
    throw new Error(`Slack API error: ${payload.error}`);
  }

  return payload;
}

function formatListLinks(listItems) {
  if (listItems.length === 0) {
    return "No accessible list items found.";
  }

  return listItems
    .map((item) =>
      `• <https://slack.com/lists/${item.listId}/item/${item.itemId}|${item.title}>`
    )
    .join("\n");
}

module.exports = {
  formatListLinks,
  sendSlackMessage,
  verifySlackSignature,
};
