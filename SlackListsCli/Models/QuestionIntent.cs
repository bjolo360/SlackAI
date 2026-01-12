using System.Text.Json.Serialization;

namespace SlackListsCli.Models;

public sealed record QuestionIntent
{
    [JsonPropertyName("intent")]
    public string Intent { get; init; } = "unknown";

    [JsonPropertyName("list_name")]
    public string ListName { get; init; } = string.Empty;

    [JsonPropertyName("person_name")]
    public string PersonName { get; init; } = string.Empty;

    public static QuestionIntent Unknown() => new();
}
