namespace FtapFaqChatbot;

public partial class FtapFaqChatPage : ContentPage
{
    private readonly FtapFaqChatClient _chatClient;
    private readonly string _sessionId = Guid.NewGuid().ToString("N");

    public FtapFaqChatPage(FtapFaqChatClient chatClient)
    {
        InitializeComponent();
        _chatClient = chatClient;
    }

    private async void OnAskClicked(object sender, EventArgs e)
    {
        await AskCurrentQuestionAsync();
    }

    private async void OnQuestionCompleted(object sender, EventArgs e)
    {
        await AskCurrentQuestionAsync();
    }

    private async Task AskCurrentQuestionAsync()
    {
        var question = QuestionEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(question))
        {
            return;
        }

        QuestionEntry.IsEnabled = false;
        AnswerLabel.Text = "Checking...";

        try
        {
            var response = await _chatClient.AskAsync(question, _sessionId);
            AnswerLabel.Text = response.Answer;
            QuestionEntry.Text = string.Empty;
        }
        catch (Exception ex)
        {
            AnswerLabel.Text = $"Unable to reach FTAP AI Chatbot. {ex.Message}";
        }
        finally
        {
            QuestionEntry.IsEnabled = true;
            QuestionEntry.Focus();
        }
    }
}
