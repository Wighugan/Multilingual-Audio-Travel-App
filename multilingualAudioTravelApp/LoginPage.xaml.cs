namespace multilingualAudioTravelApp;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
    }

    private void OnLoginClicked(object sender, EventArgs e)
    {
        Preferences.Set("isLoggedIn", true);

        Application.Current.MainPage = new AppShell();

    }

}
