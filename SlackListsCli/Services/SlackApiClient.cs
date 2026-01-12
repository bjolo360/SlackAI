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
        SlackApiException? lastError = null;
        foreach (var method in GetListMethodCandidates())
        {
            try
            {
                var response = await CallApiAsync<SlackListListResponse>(method);
                return response.Lists;
            }
            catch (SlackApiException ex) when (IsMissingListIdError(ex))
            {
                lastError = ex;
            }
        }

        if (lastError is not null)
        {
            throw lastError;
        }

        return Array.Empty<SlackList>();
    }

    public async Task<IReadOnlyList<SlackListItem>> GetListItemsAsync(string listId)
    {
        var response = await CallApiAsync<SlackListItemsResponse>(_settings.ListsItemsMethod, new Dictionary<string, string>
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
            var error = apiResponse.Error ?? "Unknown error";
            var messages = apiResponse.ResponseMetadata?.Messages ?? new List<string>();
            var details = messages.Count > 0
                ? $" Details: {string.Join(" ", messages)}"
                : string.Empty;

            var message = error switch
            {
                "unknown_method" => "Slack API error: unknown_method. Your workspace may not have the Slack Lists API enabled, or the app is missing access to Lists.",
                "invalid_arguments" => $"Slack API error: invalid_arguments.{details}",
                _ => $"Slack API error: {error}.{details}"
            };

            throw new SlackApiException(message, error, messages);
        }

        return result;
    }

    private IEnumerable<string> GetListMethodCandidates()
    {
        var methods = new[]
        {
            _settings.ListsListMethod,
            "slackLists.list",
            "lists.list"
        };

        return methods.Where(method => !string.IsNullOrWhiteSpace(method))
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsMissingListIdError(SlackApiException ex)
    {
        if (!string.Equals(ex.Error, "invalid_arguments", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return ex.Messages.Any(message =>
            message.Contains("missing required field: list_id", StringComparison.OrdinalIgnoreCase)
            || message.Contains("missing required field: id", StringComparison.OrdinalIgnoreCase));
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
