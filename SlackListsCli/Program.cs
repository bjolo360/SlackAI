using SlackListsCli.Models;
using SlackListsCli.Services;

var settings = SlackSettings.FromEnvironment();
if (!settings.IsValid)
{
    Console.Error.WriteLine("Missing Slack configuration. Set SLACK_BOT_TOKEN and optionally SLACK_WORKSPACE_DOMAIN.");
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

var httpClient = new HttpClient
{
    BaseAddress = new Uri("https://slack.com/api/")
};

var slackClient = new SlackApiClient(httpClient, settings);
var router = new NaturalLanguageRouter(slackClient);

var response = await router.RouteAsync(question.Question);
Console.WriteLine(response);

return 0;
