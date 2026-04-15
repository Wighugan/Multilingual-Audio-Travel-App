using multilingualAudioTravelApp.Services;

namespace multilingualAudioTravelApp
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            bool isLoggedIn = Preferences.Get("isLoggedIn", false);

            if (isLoggedIn)
            {
                MainPage = new AppShell();
            }
            else
            {
                MainPage = new NavigationPage(new LoginPage());
            }
        }
        protected override void OnAppLinkRequestReceived(Uri uri)
        {
            base.OnAppLinkRequestReceived(uri);

            if (uri.Scheme.ToLower() == "vktour" && uri.Host.ToLower() == "poi")
            {
                string idStr = uri.Query.Replace("?id=", "");

                if (!string.IsNullOrEmpty(idStr))
                {
                    Preferences.Set("ScannedPoiId", idStr);

                    Shell.Current.GoToAsync("//ExplorePage");
                }
            }
        }
        protected override async void OnSleep()
        {
            base.OnSleep();

            int userId = Preferences.Get("userId", 0);
            if (userId > 0)
            {
                var signalR = IPlatformApplication.Current?.Services.GetService<SignalRService>();
                if (signalR != null)
                {
                    await signalR.DisconnectAsync(userId);
                }
            }
        }

        protected override async void OnResume()
        {
            base.OnResume();

            int userId = Preferences.Get("userId", 0);
            if (userId > 0)
            {
                var signalR = IPlatformApplication.Current?.Services.GetService<SignalRService>();
                if (signalR != null)
                {
                    await signalR.ConnectAsync(userId);
                }
            }
        }
    }
}