using System.Net.Http.Json;
using System.Text.Json;

namespace FtapFaqChatbot;

public sealed class FtapFaqChatClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly Uri _webhookUri;

    public FtapFaqChatClient(HttpClient httpClient, string webhookUrl)
    {
        _httpClient = httpClient;
        _webhookUri = new Uri(webhookUrl, UriKind.Absolute);
    }

    public async Task<FtapFaqChatResponse> AskAsync(
        string message,
        string? sessionId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Message is required.", nameof(message));
        }

        var request = new FtapFaqChatRequest(message.Trim(), sessionId ?? "maui-demo");
        using var response = await _httpClient.PostAsJsonAsync(_webhookUri, request, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        var answer = await response.Content.ReadFromJsonAsync<FtapFaqChatResponse>(JsonOptions, cancellationToken);
        return answer ?? throw new InvalidOperationException("n8n returned an empty chatbot response.");
    }
}

public sealed record FtapFaqChatRequest(string Message, string SessionId);

public sealed record FtapFaqChatResponse(
    string SessionId,
    string Answer,
    double Confidence,
    string Source,
    FtapFaqMatch? MatchedFaq,
    IReadOnlyList<FtapFaqCitation> Citations,
    IReadOnlyList<string> SuggestedQuestions,
    DateTimeOffset GeneratedAt);

public sealed record FtapFaqMatch(int Number, string Question, double Score);

public sealed record FtapFaqCitation(int Number, string Question);
