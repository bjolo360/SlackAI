using System.Text.RegularExpressions;
using SlackListsCli.Models;

namespace SlackListsCli.Services;

public sealed class NaturalLanguageRouter
{
    private static readonly Regex UnresolvedRegex = new(
        "how many unresolved items are there in the list (?<list>.+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex PersonRegex = new(
        "what is (person )?(?<person>.+) working on",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly SlackApiClient _slackApiClient;

    public NaturalLanguageRouter(SlackApiClient slackApiClient)
    {
        _slackApiClient = slackApiClient;
    }

    public async Task<string> RouteAsync(string question)
    {
        if (TryParseUnresolved(question, out var listName))
        {
            return await HandleUnresolvedAsync(listName);
        }

        if (TryParsePerson(question, out var personName))
        {
            return await HandlePersonAsync(personName);
        }

        return "I couldn't understand that question. Try asking about unresolved list items or what someone is working on.";
    }

    private async Task<string> HandleUnresolvedAsync(string listName)
    {
        SlackList? list;
        try
        {
            list = await _slackApiClient.FindListByNameAsync(listName);
        }
        catch (Exception ex)
        {
            return $"Slack API error while looking up lists: {ex.Message}";
        }

        if (list is null)
        {
            return $"I couldn't find a list named '{listName}'.";
        }

        IReadOnlyList<SlackListItem> items;
        try
        {
            items = await _slackApiClient.GetListItemsAsync(list.Id);
        }
        catch (Exception ex)
        {
            return $"Slack API error while loading list items: {ex.Message}";
        }

        var unresolved = items.Count(item => !item.Status.Equals("resolved", StringComparison.OrdinalIgnoreCase));
        return $"There are {unresolved} unresolved items in '{list.Name}'.";
    }

    private async Task<string> HandlePersonAsync(string personName)
    {
        SlackUser? user;
        try
        {
            user = await _slackApiClient.FindUserByNameAsync(personName);
        }
        catch (Exception ex)
        {
            return $"Slack API error while looking up users: {ex.Message}";
        }

        if (user is null)
        {
            return $"I couldn't find a user named '{personName}'.";
        }

        IReadOnlyList<SlackListItem> assignments;
        try
        {
            assignments = await _slackApiClient.GetAssignmentsForUserAsync(user.Id);
        }
        catch (Exception ex)
        {
            return $"Slack API error while loading assignments: {ex.Message}";
        }

        if (assignments.Count == 0)
        {
            return $"{user.RealName} doesn't have any assigned list items.";
        }

        var lines = assignments
            .Select(item => $"- {item.Title} (List: {item.ListName}, Status: {item.Status})")
            .ToList();

        return $"{user.RealName} is working on:\n{string.Join("\n", lines)}";
    }

    private static bool TryParseUnresolved(string question, out string listName)
    {
        var match = UnresolvedRegex.Match(question);
        if (match.Success)
        {
            listName = match.Groups["list"].Value.Trim();
            return true;
        }

        listName = string.Empty;
        return false;
    }

    private static bool TryParsePerson(string question, out string personName)
    {
        var match = PersonRegex.Match(question);
        if (match.Success)
        {
            personName = match.Groups["person"].Value.Trim();
            return true;
        }

        personName = string.Empty;
        return false;
    }
}
