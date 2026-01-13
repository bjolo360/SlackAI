using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SlackListsCli.Models;

namespace SlackListsCli.Services;

public sealed class OpenAiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly OpenAiSettings _settings;

    public OpenAiClient(HttpClient httpClient, OpenAiSettings settings)
    {
        _httpClient = httpClient;
        _settings = settings;
    }

    public async Task<QuestionIntent> ClassifyQuestionAsync(string question)
    {
        var payload = new OpenAiChatRequest
        {
            Model = _settings.Model,
            Messages =
            [
                new OpenAiChatMessage
                {
                    Role = "system",
                    Content = "You classify Slack Lists questions. Return ONLY minified JSON with keys: intent, list_name, person_name. intent values: unresolved_count, person_assignments, unknown."
                },
                new OpenAiChatMessage
                {
                    Role = "user",
                    Content = $"Question: {question}\nExamples:\n1) How many unresolved items are there in the list Product Launch -> {{\"intent\":\"unresolved_count\",\"list_name\":\"Product Launch\",\"person_name\":\"\"}}\n2) What is Ada Lovelace working on -> {{\"intent\":\"person_assignments\",\"list_name\":\"\",\"person_name\":\"Ada Lovelace\"}}\n3) Show me everything -> {{\"intent\":\"unknown\",\"list_name\":\"\",\"person_name\":\"\"}}"
                }
            ]
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request);
        var responsePayload = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"OpenAI API error: {response.StatusCode} {responsePayload}");
        }

        var chatResponse = JsonSerializer.Deserialize<OpenAiChatResponse>(responsePayload, JsonOptions);
        var content = chatResponse?.Choices.FirstOrDefault()?.Message.Content;
        if (string.IsNullOrWhiteSpace(content))
        {
            return QuestionIntent.Unknown();
        }

        content = NormalizeJson(content);
        try
        {
            var intent = JsonSerializer.Deserialize<QuestionIntent>(content, JsonOptions);
            return intent ?? QuestionIntent.Unknown();
        }
        catch (JsonException)
        {
            return QuestionIntent.Unknown();
        }
    }

    private static string NormalizeJson(string content)
    {
        var trimmed = content.Trim();
        if (!trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            return trimmed;
        }

        var lines = trimmed.Split('\n');
        var jsonLines = lines
            .Where(line => !line.TrimStart().StartsWith("```", StringComparison.Ordinal))
            .ToArray();
        return string.Join('\n', jsonLines).Trim();
    }

    private sealed record OpenAiChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; init; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<OpenAiChatMessage> Messages { get; init; } = new();
    }

    private sealed record OpenAiChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; init; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; init; } = string.Empty;
    }

    private sealed record OpenAiChatResponse
    {
        [JsonPropertyName("choices")]
        public List<OpenAiChatChoice> Choices { get; init; } = new();
    }

    private sealed record OpenAiChatChoice
    {
        [JsonPropertyName("message")]
        public OpenAiChatMessage Message { get; init; } = new();
    }
}
