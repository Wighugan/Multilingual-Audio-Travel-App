namespace multilingualAudioTravelApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(EditProfilePage), typeof(EditProfilePage));

        }
    }
}
