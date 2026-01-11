const bodyParser = require("body-parser");
const dotenv = require("dotenv");
const express = require("express");
const fetch = require("node-fetch");

const { handleNlQuery } = require("./nl");
const { isUserAllowed } = require("./permissions");
const { formatListLinks, sendSlackMessage, verifySlackSignature } = require("./slack");

dotenv.config();

const app = express();
const port = process.env.PORT || 3000;

const rawBodySaver = (req, _res, buf) => {
  if (buf && buf.length) {
    req.rawBody = buf.toString("utf8");
  }
};

app.use(bodyParser.json({ verify: rawBodySaver }));
app.use(bodyParser.urlencoded({ extended: true, verify: rawBodySaver }));

function requireSlackSignature(req, res, next) {
  if (!verifySlackSignature(req)) {
    res.status(401).send("Invalid Slack signature.");
    return;
  }

  next();
}

async function routeToNlEndpoint(query, userId) {
  const endpoint =
    process.env.NL_QUERY_URL || `http://localhost:${port}/api/nl-query`;

  const response = await fetch(endpoint, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ query, userId }),
  });

  if (!response.ok) {
    throw new Error(`NL endpoint error: ${response.status}`);
  }

  return response.json();
}

app.post("/api/nl-query", (req, res) => {
  const { query, userId } = req.body;

  if (!query || !userId) {
    res.status(400).json({ error: "Missing query or userId." });
    return;
  }

  if (!isUserAllowed(userId)) {
    res.status(403).json({ error: "User not authorized." });
    return;
  }

  const payload = handleNlQuery({ query, userId });
  res.json(payload);
});

app.post("/slack/events", requireSlackSignature, async (req, res) => {
  const body = req.body;

  if (body.type === "url_verification") {
    res.json({ challenge: body.challenge });
    return;
  }

  if (body.event && body.event.type === "message" && !body.event.bot_id) {
    res.status(200).send();

    try {
      if (!isUserAllowed(body.event.user)) {
        await sendSlackMessage({
          channel: body.event.channel,
          text: "You do not have access to this workspace data.",
        });
        return;
      }

      const result = await routeToNlEndpoint(body.event.text, body.event.user);
      const responseText = formatListLinks(result.results || []);

      await sendSlackMessage({
        channel: body.event.channel,
        text: responseText,
      });
    } catch (error) {
      console.error("Slack event handling failed:", error);
      await sendSlackMessage({
        channel: body.event.channel,
        text: "Something went wrong handling your request.",
      });
    }

    return;
  }

  res.status(200).send();
});

app.post("/slack/commands", requireSlackSignature, async (req, res) => {
  const { command, text, user_id: userId } = req.body;

  if (command !== "/list-ask") {
    res.status(200).send("Unknown command.");
    return;
  }

  if (!isUserAllowed(userId)) {
    res.status(200).send("You do not have access to this workspace data.");
    return;
  }

  try {
    const result = await routeToNlEndpoint(text, userId);
    const responseText = formatListLinks(result.results || []);

    res.status(200).json({
      response_type: "ephemeral",
      text: responseText,
    });
  } catch (error) {
    console.error("Slash command failed:", error);
    res.status(200).json({
      response_type: "ephemeral",
      text: "Something went wrong handling your request.",
    });
  }
});

app.listen(port, () => {
  console.log(`SlackAI server listening on ${port}`);
});
