using SlackListsCli.Models;
using SlackListsCli.Services;

var settings = SlackSettings.FromEnvironment();
if (!settings.IsValid)
{
    Console.Error.WriteLine("Missing Slack configuration. Set SLACK_BOT_TOKEN and optionally SLACK_WORKSPACE_DOMAIN.");
    return 1;
}

var openAiSettings = OpenAiSettings.FromEnvironment();
if (!openAiSettings.IsValid)
{
    Console.Error.WriteLine("Missing OpenAI configuration. Set OPENAI_API_KEY and optionally OPENAI_MODEL.");
    return 1;
}

var question = QuestionInput.FromArgs(args);
if (string.IsNullOrWhiteSpace(question.Question))
{
    Console.WriteLine("Ask a question about Slack Lists.");
    Console.WriteLine("Examples:");
    Console.WriteLine("  slack-lists --question \"How many unresolved items are there in the list Product Launch\"");
    Console.WriteLine("  slack-lists --question \"What is person Y working on\"");
    return 0;
}

var slackHttpClient = new HttpClient
{
    BaseAddress = new Uri("https://slack.com/api/")
};

var openAiHttpClient = new HttpClient
{
    BaseAddress = new Uri("https://api.openai.com/v1/")
};

var slackClient = new SlackApiClient(slackHttpClient, settings);
var openAiClient = new OpenAiClient(openAiHttpClient, openAiSettings);
var router = new NaturalLanguageRouter(slackClient, openAiClient);

var response = await router.RouteAsync(question.Question);
Console.WriteLine(response);

return 0;
