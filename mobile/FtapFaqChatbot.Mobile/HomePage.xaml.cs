namespace FtapFaqChatbot.Mobile;

public partial class HomePage : ContentPage
{
	public HomePage()
	{
		InitializeComponent();
	}

	private async void OnChatFabClicked(object? sender, EventArgs e)
	{
		await Navigation.PushModalAsync(new MainPage());
	}
}
