using SlackListsCli.Models;

namespace SlackListsCli.Services;

public sealed class NaturalLanguageRouter
{
    private readonly SlackApiClient _slackApiClient;
    private readonly OpenAiClient _openAiClient;

    public NaturalLanguageRouter(SlackApiClient slackApiClient, OpenAiClient openAiClient)
    {
        _slackApiClient = slackApiClient;
        _openAiClient = openAiClient;
    }

    public async Task<string> RouteAsync(string question)
    {
        QuestionIntent intent;
        try
        {
            intent = await _openAiClient.ClassifyQuestionAsync(question);
        }
        catch (Exception ex)
        {
            return $"OpenAI error while interpreting the question: {ex.Message}";
        }

        return intent.Intent switch
        {
            "unresolved_count" when !string.IsNullOrWhiteSpace(intent.ListName)
                => await HandleUnresolvedAsync(intent.ListName),
            "person_assignments" when !string.IsNullOrWhiteSpace(intent.PersonName)
                => await HandlePersonAsync(intent.PersonName),
            _ => "I couldn't understand that question. Try asking about unresolved list items or what someone is working on."
        };
    }

    private async Task<string> HandleUnresolvedAsync(string listName)
    {
        SlackList? list;
        try
        {
            if (_slackApiClient.TryResolveListId(listName, out var mappedListId))
            {
                return await BuildUnresolvedResponseAsync(listName, mappedListId);
            }

            list = await _slackApiClient.FindListByNameAsync(listName);
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("invalid_arguments", StringComparison.OrdinalIgnoreCase)
                && ex.Message.Contains("list_id", StringComparison.OrdinalIgnoreCase))
            {
                return "Slack Lists API requires a list_id. Set SLACK_LIST_ID_MAP to map list names to IDs or provide SLACK_DEFAULT_LIST_ID.";
            }

            return $"Slack API error while looking up lists: {ex.Message}";
        }

        if (list is null)
        {
            return $"I couldn't find a list named '{listName}'.";
        }

        return await BuildUnresolvedResponseAsync(list.Name, list.Id);
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
            if (ex.Message.Contains("invalid_arguments", StringComparison.OrdinalIgnoreCase)
                && ex.Message.Contains("list_id", StringComparison.OrdinalIgnoreCase))
            {
                return "Slack Lists API requires list_id values to fetch list items. Set SLACK_LIST_ID_MAP with list names to IDs to enable per-user lookups.";
            }

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

    private async Task<string> BuildUnresolvedResponseAsync(string listName, string listId)
    {
        IReadOnlyList<SlackListItem> items;
        try
        {
            items = await _slackApiClient.GetListItemsAsync(listId);
        }
        catch (Exception ex)
        {
            return $"Slack API error while loading list items: {ex.Message}";
        }

        var unresolved = items.Count(item => !item.Status.Equals("resolved", StringComparison.OrdinalIgnoreCase));
        return $"There are {unresolved} unresolved items in '{listName}'.";
    }

    
}
