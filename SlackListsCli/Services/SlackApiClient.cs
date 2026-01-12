using System.Net.Http.Headers;
using System.Text.Json;
using SlackListsCli.Models;

namespace SlackListsCli.Services;

public sealed class SlackApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly SlackSettings _settings;

    public SlackApiClient(HttpClient httpClient, SlackSettings settings)
    {
        _httpClient = httpClient;
        _settings = settings;
    }

    public async Task<IReadOnlyList<SlackList>> GetListsAsync()
    {
        var response = await CallApiAsync<SlackListListResponse>("lists.list");
        return response.Lists;
    }

    public async Task<IReadOnlyList<SlackListItem>> GetListItemsAsync(string listId)
    {
        var response = await CallApiAsync<SlackListItemsResponse>("lists.items", new Dictionary<string, string>
        {
            ["list_id"] = listId
        });

        return response.Items
            .Select(item => new SlackListItem(item.Id, item.Title, item.Status, item.AssigneeUserId, listId, string.Empty))
            .ToList();
    }

    public async Task<SlackList?> FindListByNameAsync(string listName)
    {
        var lists = await GetListsAsync();
        return lists.FirstOrDefault(list => list.Name.Equals(listName, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<SlackUser?> FindUserByNameAsync(string name)
    {
        var response = await CallApiAsync<SlackUsersResponse>("users.list");
        return response.Members.FirstOrDefault(user =>
            user.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
            || user.RealName.Equals(name, StringComparison.OrdinalIgnoreCase)
            || user.Profile.DisplayName.Equals(name, StringComparison.OrdinalIgnoreCase)
            || user.Profile.Email.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IReadOnlyList<SlackListItem>> GetAssignmentsForUserAsync(string userId)
    {
        var lists = await GetListsAsync();
        var listLookup = lists.ToDictionary(list => list.Id, list => list.Name);
        var assignments = new List<SlackListItem>();

        foreach (var list in lists)
        {
            var items = await GetListItemsAsync(list.Id);
            assignments.AddRange(items
                .Where(item => item.AssigneeUserId.Equals(userId, StringComparison.OrdinalIgnoreCase))
                .Select(item => item with { ListName = listLookup[item.ListId] }));
        }

        return assignments;
    }

    private async Task<T> CallApiAsync<T>(string method, IDictionary<string, string>? query = null)
    {
        var requestUri = BuildRequestUri(method, query);
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.BotToken);

        using var response = await _httpClient.SendAsync(request);
        var payload = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<T>(payload, JsonOptions);

        if (result is null)
        {
            throw new InvalidOperationException("Slack API returned an empty response.");
        }

        var apiResponse = result as SlackApiResponseBase;
        if (apiResponse is { Ok: false })
        {
            throw new InvalidOperationException($"Slack API error: {apiResponse.Error ?? "Unknown error"}.");
        }

        return result;
    }

    private static string BuildRequestUri(string method, IDictionary<string, string>? query)
    {
        if (query is null || query.Count == 0)
        {
            return method;
        }

        var encoded = string.Join("&", query.Select(pair =>
            $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));

        return $"{method}?{encoded}";
    }
}
