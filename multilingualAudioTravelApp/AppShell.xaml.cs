namespace multilingualAudioTravelApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(EditProfilePage), typeof(EditProfilePage));
            Routing.RegisterRoute(nameof(QRScanPage), typeof(QRScanPage));
            Routing.RegisterRoute(nameof(MyQRPage), typeof(MyQRPage));
        }
    }
}
