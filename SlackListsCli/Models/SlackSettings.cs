namespace SlackListsCli.Models;

public sealed record SlackSettings(
    string BotToken,
    string WorkspaceDomain,
    string ListsListMethod,
    string ListsItemsMethod)
{
    public bool IsValid => !string.IsNullOrWhiteSpace(BotToken);

    public static SlackSettings FromEnvironment()
    {
        var token = Environment.GetEnvironmentVariable("SLACK_BOT_TOKEN") ?? string.Empty;
        var domain = Environment.GetEnvironmentVariable("SLACK_WORKSPACE_DOMAIN") ?? string.Empty;
        var listsListMethod = Environment.GetEnvironmentVariable("SLACK_LISTS_LIST_METHOD") ?? "slackLists.list";
        var listsItemsMethod = Environment.GetEnvironmentVariable("SLACK_LISTS_ITEMS_METHOD") ?? "slackLists.items.list";
        return new SlackSettings(token, domain, listsListMethod, listsItemsMethod);
    }
}
