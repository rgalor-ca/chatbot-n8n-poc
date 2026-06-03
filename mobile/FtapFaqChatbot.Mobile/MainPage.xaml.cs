using System.Collections.ObjectModel;

namespace FtapFaqChatbot.Mobile;

public partial class MainPage : ContentPage
{
	private static readonly IReadOnlyList<FaqSuggestion> AllSuggestions =
	[
		new(1, "What are the credentials needed for the registration on the iAMS Mobile App?"),
		new(2, "What is the PIN Code on the iAMS Mobile App?"),
		new(3, "Where to message if there is a need for an emergency access?"),
		new(4, "Where to call if there is a need of an emergency access after office hours, weekends, and holidays?"),
		new(5, "What should we do if we receive an error of “No Access Right on specified Asset Point”"),
		new(6, "Where to send inquiries, requests, or escalations?"),
		new(7, "How to add vendor organization on iAMS Web Portal?"),
		new(8, "What to do if tickets cannot be found on the ticket list or on the iAMS web portal?"),
		new(9, "Who to call if the site as an issue with the lessor?"),
		new(10, "What to do if we need to use the ticket again for the access?"),
		new(11, "What do we need to upload on the shared OneDrive?"),
		new(12, "Why can’t we request access for installation and dismantling activities?"),
		new(13, "Why can't we request access for pullout activities?"),
		new(14, "What should we do if we receive an error of “Not Able to Connect on Relay Server” or “Server is Busy”?"),
		new(15, "What should we do if we receive an error of \"Check-in successful for asset point. Asset point already opened. Please proceed to work\""),
		new(16, "What should we do if we receive an error of \" Could not able to find Access Point - Try again\""),
		new(17, "What should we do if we receive an error of \" The previous site not close yet, cannot request another site before closing it\"")
	];

	private static readonly IReadOnlyDictionary<int, string> ExactAnswers = new Dictionary<int, string>
	{
		[1] = "Choose Acsys Mobile iAMS from the available list. Click Select Cloud and choose Custom, switch the tab to https and enter below details. Click register, OTP will be received enter it, enter your Password, and click register you’ll be registered successfully.\nKindly input these details:\nCloud ID: ftaprelay.frontiertowersphilippines.com\nServer ID: ftapiams\nUse registered email address.",
		[2] = "1234",
		[3] = "Strictly for Direct Messages Only\n∙  FTAP Site Access No.:  09177149799 (WhatsApp)\n∙  FTAP Site Access No.:  09992293099 (WhatsApp)",
		[4] = "∙  Site Access Landline No. - (02) 82504659\n∙  Site Access Globe Hotline No. - 180015500101",
		[5] = "Kindly check first if your ticket has been approved and if the registered personnel’s area of responsibility has been updated on the User’s geography.\nIf all these are correct and issue persist, kindly coordinate with the FTAP WhatsApp hotline and send the screenshot.",
		[6] = "For GLOBE customers:  globesiteaccess@frontiertowersphilippines.com",
		[7] = "Kindly email us the details of the vendor organization.\nFor Add Organization,\nPlease fill the details below:\nVendor Name:\nVendor Contact Person:\nVendor Email:\nVendor Contact Number:",
		[8] = "Re-file or create another work order/ticket number and email us for approval.",
		[9] = "FTAP Estate Management\n•   North NCR and North Luzon: 0905 558 0676\n•   South NCR and South Luzon: 0998 571 5446\n•   Visayas Area: 0968 897 2421\n•   Mindanao Area: 0961 709 3036",
		[10] = "Kindly remind the team to uncheck the “CLOSE TICKET” box. Afterwards, go to Menu > Ticket list, and click “REOPEN” so that they can reuse the ticket until its validity date. Maximum validity of access ticket is 10 days. Request a new ticket access if the activity would go beyond the 10 days.",
		[11] = "For new registration:\n-      Company ID\n-      Working at Height certificate (WAH)\n-      Certificate of Employment (COE)\n-      NBI Clearance\nFor access request:\n-      MOP/SOW (Detailed information of activity)\n-      RAAWA (Globe)",
		[12] = "You need to provide the approved change request application number.",
		[13] = "Fill out the FTAP Extraction Form, complete all details and have it signed to your authorized signatories. Email the form to Site Access Team and they will route it for approval internally. Once approved, you may now file for work order/ticket access.",
		[14] = "Kindly check if you’re using the new mobile application.\nIf all these are correct and issue persist, kindly coordinate with the FTAP WhatsApp hotline and send the screenshot for further analysis.",
		[15] = "Kindly coordinate with the FTAP WhatsApp hotline and send the screenshot for further analysis.",
		[16] = "Kindly coordinate with the FTAP WhatsApp hotline and send the screenshot for further analysis.",
		[17] = "Kindly coordinate with the FTAP WhatsApp hotline and send the screenshot for further analysis."
	};

	private const string OutOfScopeAnswer = "I can only answer questions covered by the FTAP SAM FAQ knowledge base. Please ask about iAMS registration, PIN, emergency access, tickets, OneDrive uploads, relay/server errors, lessor/site access, or other FTAP site access FAQ topics.";

	private static readonly IReadOnlySet<string> StopTokens = new HashSet<string>(StringComparer.Ordinal)
	{
		"a", "an", "and", "are", "as", "do", "for", "how", "if", "is", "it", "of", "on", "or", "that", "the", "this", "to", "we", "what", "when", "where", "who", "why", "with"
	};

	private static readonly IReadOnlySet<string> InScopeTokens = new HashSet<string>(StringComparer.Ordinal)
	{
		"access", "acsys", "app", "approval", "approved", "area", "asset", "busy", "certificate", "check", "checkin", "clearance", "cloud", "coe", "code", "company", "connect", "credentials", "dismantling", "email", "emergency", "employment", "escalations", "extraction", "find", "ftap", "geography", "globe", "hotline", "iams", "inquiries", "installation", "landline", "lessor", "mobile", "mop", "nbi", "onedrive", "organization", "otp", "password", "pin", "portal", "pullout", "raawa", "registration", "relay", "reopen", "requests", "server", "site", "sow", "ticket", "upload", "vendor", "wah", "whatsapp", "work"
	};

	private static readonly IReadOnlyList<string> InScopePhrases =
	[
		"after office",
		"asset point",
		"check in",
		"check-in",
		"close ticket",
		"could not able to find",
		"no access right",
		"not able to connect",
		"previous site",
		"server is busy",
		"specified asset point",
		"ticket list",
		"work order"
	];

	private readonly FtapFaqChatClient _chatClient = new();
	private readonly string _sessionId = Guid.NewGuid().ToString("N");
	private bool _selectingSuggestion;

	public ObservableCollection<ChatMessage> Messages { get; } = [];

	public ObservableCollection<FaqSuggestion> FilteredSuggestions { get; } = [];

	public MainPage()
	{
		InitializeComponent();
		BindingContext = this;
	}

	private async void OnSendClicked(object? sender, EventArgs e)
	{
		await AskAsync(QuestionEntry.Text);
	}

	private async void OnQuestionCompleted(object? sender, EventArgs e)
	{
		await AskAsync(QuestionEntry.Text);
	}

	private async void OnCloseClicked(object? sender, EventArgs e)
	{
		await CloseModalAsync();
	}

	private async void OnCloseTapped(object? sender, TappedEventArgs e)
	{
		await CloseModalAsync();
	}

	private async Task CloseModalAsync()
	{
		if (Navigation.ModalStack.Count > 0)
		{
			await Navigation.PopModalAsync();
		}
	}

	private void OnQuestionEntryFocused(object? sender, FocusEventArgs e)
	{
		UpdateSuggestions(QuestionEntry.Text);
	}

	private void OnQuestionEntryUnfocused(object? sender, FocusEventArgs e)
	{
		HideSuggestions();
	}

	private void OnBackgroundTapped(object? sender, TappedEventArgs e)
	{
		HideSuggestions();
	}

	private void OnMessagesScrolled(object? sender, ItemsViewScrolledEventArgs e)
	{
		HideSuggestions();
	}

	private void OnQuestionTextChanged(object? sender, TextChangedEventArgs e)
	{
		if (_selectingSuggestion)
		{
			return;
		}

		UpdateSuggestions(e.NewTextValue);
	}

	private async void OnSuggestionClicked(object? sender, EventArgs e)
	{
		if ((sender as BindableObject)?.BindingContext is not FaqSuggestion suggestion)
		{
			return;
		}

		_selectingSuggestion = true;
		HideSuggestions();
		QuestionEntry.Text = suggestion.Question;
		_selectingSuggestion = false;
		await AskAsync(suggestion.Question);
	}

	private async Task AskAsync(string? question)
	{
		question = question?.Trim();
		if (string.IsNullOrWhiteSpace(question))
		{
			return;
		}

		Messages.Add(new ChatMessage("You", question));
		QuestionEntry.Text = string.Empty;
		HideSuggestions();

		if (!IsInScopeQuestion(question))
		{
			Messages.Add(new ChatMessage("FTAP", OutOfScopeAnswer));
			MessagesView.ScrollTo(Messages.Count - 1, position: ScrollToPosition.End, animate: true);
			return;
		}

		SetBusy(true);

		try
		{
			var response = await _chatClient.AskAsync(question, _sessionId);
			var answer = response.MatchedFaq is not null && ExactAnswers.TryGetValue(response.MatchedFaq.Number, out var exactAnswer)
				? exactAnswer
				: response.Answer;
			Messages.Add(new ChatMessage("FTAP", answer));
		}
		catch (Exception ex)
		{
			Messages.Add(new ChatMessage("FTAP", $"Unable to reach the n8n FAQ workflow. {ex.Message}"));
		}
		finally
		{
			SetBusy(false);
			MessagesView.ScrollTo(Messages.Count - 1, position: ScrollToPosition.End, animate: true);
		}
	}

	private void SetBusy(bool isBusy)
	{
		SendButton.IsEnabled = !isBusy;
		QuestionEntry.IsEnabled = !isBusy;
		SendButton.Text = isBusy ? "..." : "Send";
	}

	private void HideSuggestions()
	{
		SuggestionsPanel.IsVisible = false;
	}

	private void UpdateSuggestions(string? value)
	{
		var query = Normalize(value);
		var suggestions = string.IsNullOrWhiteSpace(query) || !IsInScopeQuestion(query)
			? Array.Empty<FaqSuggestion>()
			: AllSuggestions
				.Select(suggestion => new
				{
					Suggestion = suggestion,
					Score = ScoreSuggestion(query, suggestion)
				})
				.Where(match => match.Score > 0)
				.OrderByDescending(match => match.Score)
				.ThenBy(match => match.Suggestion.Number)
				.Select(match => match.Suggestion)
				.Take(5)
				.ToArray();

		FilteredSuggestions.Clear();
		foreach (var suggestion in suggestions)
		{
			FilteredSuggestions.Add(suggestion);
		}

		SuggestionsPanel.IsVisible = FilteredSuggestions.Count > 0 && QuestionEntry.IsEnabled;
	}

	private static int ScoreSuggestion(string query, FaqSuggestion suggestion)
	{
		var tokens = Tokenize(query).ToArray();
		if (tokens.Length == 0)
		{
			return 0;
		}

		var haystack = Normalize($"{suggestion.Number} {suggestion.Question}");
		var score = 0;
		foreach (var token in tokens)
		{
			if (haystack.Contains(token, StringComparison.Ordinal))
			{
				score += token.Length;
			}
		}

		if (query.Contains("pin", StringComparison.Ordinal) && suggestion.Number == 2) score += 20;
		if (query.Contains("emergency", StringComparison.Ordinal) && suggestion.Number is 3 or 4) score += 16;
		if (query.Contains("onedrive", StringComparison.Ordinal) && suggestion.Number == 11) score += 20;
		if (query.Contains("upload", StringComparison.Ordinal) && suggestion.Number == 11) score += 14;
		if (query.Contains("relay", StringComparison.Ordinal) && suggestion.Number == 14) score += 14;
		if (query.Contains("lessor", StringComparison.Ordinal) && suggestion.Number == 9) score += 14;
		if (query.Contains("reopen", StringComparison.Ordinal) && suggestion.Number == 10) score += 14;

		return score;
	}

	private static bool IsInScopeQuestion(string question)
	{
		var normalized = Normalize(question);
		if (string.IsNullOrWhiteSpace(normalized))
		{
			return false;
		}

		if (AllSuggestions.Any(suggestion => Normalize(suggestion.Question) == normalized))
		{
			return true;
		}

		if (AllSuggestions.Any(suggestion => normalized == suggestion.Number.ToString() || normalized == $"faq {suggestion.Number}"))
		{
			return true;
		}

		if (InScopePhrases.Any(phrase => normalized.Contains(phrase, StringComparison.Ordinal)))
		{
			return true;
		}

		return Tokenize(normalized).Any(InScopeTokens.Contains);
	}

	private static IEnumerable<string> Tokenize(string? value)
	{
		return Normalize(value)
			.Split(' ', StringSplitOptions.RemoveEmptyEntries)
			.Where(token => token.Length > 1 && !StopTokens.Contains(token));
	}

	private static string Normalize(string? value)
	{
		return new string((value ?? string.Empty)
				.ToLowerInvariant()
				.Select(character => char.IsLetterOrDigit(character) ? character : ' ')
				.ToArray())
			.Replace("  ", " ")
			.Trim();
	}
}

public sealed record ChatMessage(string Sender, string Text)
{
	public bool IsUser => Sender == "You";

	public Color BubbleColor => IsUser
		? Color.FromArgb("#FFE1D0")
		: Color.FromArgb("#FFFFFF");

	public Color BorderColor => IsUser
		? Color.FromArgb("#F6B58E")
		: Color.FromArgb("#F6C7AA");

	public Color SenderColor => IsUser
		? Color.FromArgb("#8C3A15")
		: Color.FromArgb("#C85620");

	public Color TextColor => IsUser
		? Color.FromArgb("#1F2029")
		: Color.FromArgb("#1F2029");

	public Thickness BubbleMargin => IsUser
		? new Thickness(58, 5, 0, 5)
		: new Thickness(0, 5, 58, 5);
}

public sealed record FaqSuggestion(int Number, string Question);
