using System.Net.Http.Json;
using System.Text.Json;

namespace FtapFaqChatbot.Mobile;

public sealed class FtapFaqChatClient
{
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
	private static readonly Uri WebhookUri = new("https://raymondneil.app.n8n.cloud/webhook/ftap-faq-chat");

	private readonly HttpClient _httpClient = new();

	public async Task<FtapFaqChatResponse> AskAsync(
		string message,
		string sessionId,
		CancellationToken cancellationToken = default)
	{
		var request = new FtapFaqChatRequest(message.Trim(), sessionId);
		using var response = await _httpClient.PostAsJsonAsync(WebhookUri, request, JsonOptions, cancellationToken);
		response.EnsureSuccessStatusCode();

		var answer = await response.Content.ReadFromJsonAsync<FtapFaqChatResponse>(JsonOptions, cancellationToken);
		return answer ?? throw new InvalidOperationException("n8n returned an empty response.");
	}
}

public sealed record FtapFaqChatRequest(string Message, string SessionId);

public sealed record FtapFaqChatResponse(
	string SessionId,
	string Answer,
	double Confidence,
	string Source,
	FtapFaqMatch? MatchedFaq,
	IReadOnlyList<FtapFaqCitation>? Citations,
	IReadOnlyList<string>? SuggestedQuestions,
	DateTimeOffset GeneratedAt);

public sealed record FtapFaqMatch(int Number, string Question, double Score);

public sealed record FtapFaqCitation(int Number, string Question);
