namespace SlackListsCli.Models;

public sealed record OpenAiSettings(string ApiKey, string Model)
{
    public bool IsValid => !string.IsNullOrWhiteSpace(ApiKey);

    public static OpenAiSettings FromEnvironment()
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? string.Empty;
        var model = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4o-mini";
        return new OpenAiSettings(apiKey, model);
    }
}
