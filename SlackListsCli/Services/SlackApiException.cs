namespace SlackListsCli.Services;

public sealed class SlackApiException : Exception
{
    public SlackApiException(string message, string error, IReadOnlyList<string> messages)
        : base(message)
    {
        Error = error;
        Messages = messages;
    }

    public string Error { get; }

    public IReadOnlyList<string> Messages { get; }
}
