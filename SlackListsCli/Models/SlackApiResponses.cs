using System.Text.Json.Serialization;

namespace SlackListsCli.Models;

public abstract record SlackApiResponseBase
{
    [JsonPropertyName("ok")]
    public bool Ok { get; init; }

    [JsonPropertyName("error")]
    public string? Error { get; init; }

    [JsonPropertyName("response_metadata")]
    public SlackResponseMetadata? ResponseMetadata { get; init; }
}

public sealed record SlackResponseMetadata
{
    [JsonPropertyName("messages")]
    public List<string> Messages { get; init; } = new();
}

public sealed record SlackListListResponse : SlackApiResponseBase
{
    [JsonPropertyName("lists")]
    public List<SlackList> Lists { get; init; } = new();
}

public sealed record SlackListItemsResponse : SlackApiResponseBase
{
    [JsonPropertyName("items")]
    public List<SlackListItemDto> Items { get; init; } = new();
}

public sealed record SlackUsersResponse : SlackApiResponseBase
{
    [JsonPropertyName("members")]
    public List<SlackUser> Members { get; init; } = new();
}

public sealed record SlackListItemDto
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("assignee")]
    public string AssigneeUserId { get; init; } = string.Empty;
}
