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

    }
}