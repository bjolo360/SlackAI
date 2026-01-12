using System.Text.Json.Serialization;

namespace SlackListsCli.Models;

public sealed record SlackList
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
}

public sealed record SlackListItem(
    string Id,
    string Title,
    string Status,
    string AssigneeUserId,
    string ListId,
    string ListName);

public sealed record SlackUser
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("real_name")]
    public string RealName { get; init; } = string.Empty;

    [JsonPropertyName("profile")]
    public SlackUserProfile Profile { get; init; } = new();
}

public sealed record SlackUserProfile
{
    [JsonPropertyName("display_name")]
    public string DisplayName { get; init; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;
}
