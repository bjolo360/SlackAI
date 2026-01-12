namespace SlackListsCli.Models;

public sealed record SlackSettings(
    string BotToken,
    string WorkspaceDomain)
{
    public bool IsValid => !string.IsNullOrWhiteSpace(BotToken);

    public static SlackSettings FromEnvironment()
    {
        var token = Environment.GetEnvironmentVariable("SLACK_BOT_TOKEN") ?? string.Empty;
        var domain = Environment.GetEnvironmentVariable("SLACK_WORKSPACE_DOMAIN") ?? string.Empty;
        return new SlackSettings(token, domain);
    }
}
