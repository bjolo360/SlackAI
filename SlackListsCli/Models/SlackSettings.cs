namespace SlackListsCli.Models;

public sealed record SlackSettings(
    string BotToken,
    string WorkspaceDomain,
    string ListsListMethod,
    string ListsItemsMethod,
    string DefaultListId,
    IReadOnlyDictionary<string, string> ListIdMap)
{
    public bool IsValid => !string.IsNullOrWhiteSpace(BotToken);

    public static SlackSettings FromEnvironment()
    {
        var token = Environment.GetEnvironmentVariable("SLACK_BOT_TOKEN") ?? string.Empty;
        var domain = Environment.GetEnvironmentVariable("SLACK_WORKSPACE_DOMAIN") ?? string.Empty;
        var listsListMethod = Environment.GetEnvironmentVariable("SLACK_LISTS_LIST_METHOD") ?? "slackLists.items.list";
        var listsItemsMethod = Environment.GetEnvironmentVariable("SLACK_LISTS_ITEMS_METHOD") ?? "slackLists.items.info";
        var defaultListId = Environment.GetEnvironmentVariable("SLACK_DEFAULT_LIST_ID") ?? string.Empty;
        var listIdMap = ParseListIdMap(Environment.GetEnvironmentVariable("SLACK_LIST_ID_MAP"));
        return new SlackSettings(token, domain, listsListMethod, listsItemsMethod, defaultListId, listIdMap);
    }

    private static IReadOnlyDictionary<string, string> ParseListIdMap(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var parsed = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(raw);
            return parsed is null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(parsed, StringComparer.OrdinalIgnoreCase);
        }
        catch (System.Text.Json.JsonException)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
