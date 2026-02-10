namespace multilingualAudioTravelApp;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
    }

    private void OnLoginClicked(object sender, EventArgs e)
    {
        Application.Current.MainPage = new AppShell();

    }

}
