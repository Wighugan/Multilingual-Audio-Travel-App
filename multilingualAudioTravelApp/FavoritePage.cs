using multilingualAudioTravelApp.Services;
using static multilingualAudioTravelApp.Services.PoiEntity;

namespace multilingualAudioTravelApp;

public partial class FavoritePage : ContentPage
{
    private readonly DatabaseService _dbService = new DatabaseService();
    private bool _removeTapping = false;

    // State cho popup thuyết minh
    private FavoriteEntity _selectedFav;
    private CancellationTokenSource _speechCts;

    public FavoritePage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadFavorites();
        // Update localized labels in popup in case language changed while page was inactive
        FavPlayLabel.Text = Languages.AppStrings.Play;
        FavMapLabel.Text = Languages.AppStrings.Map;
        FavCloseLabel.Text = Languages.AppStrings.Close;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopSpeech(); // dừng đọc khi rời trang
    }

    // ── Load danh sách ──────────────────────────────────────────

    private async Task LoadFavorites()
    {
        var email = Preferences.Get("userEmail", "");
        var favorites = await _dbService.GetFavoritesAsync(email);
        FavCollection.ItemsSource = favorites;
    }

    // ── Bấm vào card → mở popup ─────────────────────────────────

    // ✅ SỬA - hiện overlay loading NGAY LẬP TỨC trước khi await
    private async void OnFavoriteRowTapped(object sender, TappedEventArgs e)
    {
        if (_removeTapping) return;
        if (e.Parameter is not FavoriteEntity fav) return;

        _selectedFav = fav;

        // Hiện overlay NGAY để chặn tap bubble lên Shell
        FavPopupOverlay.IsVisible = true;
        FavPopupTitle.Text = fav.PoiName;
        FavPopupDescription.Text = fav.PoiDescription ?? "";
        FavPopupCarousel.ItemsSource = new List<string> {
        string.IsNullOrWhiteSpace(fav.PoiImage) ? "placeholder.png" : fav.PoiImage
    };
        FavPlayStopButton.Source = "play_icon.png";

        // Sau đó mới load thêm dữ liệu đầy đủ từ DB (update lại nếu có)
        var entities = await _dbService.GetAllPoisAsync();
        var poi = entities.FirstOrDefault(p => p.CurrentName == fav.PoiName);

        if (poi != null)
        {
            var images = poi.ImageUrls?.Count > 0
                ? poi.ImageUrls
                : new List<string> { poi.FullImageUrl ?? "placeholder.png" };
            FavPopupCarousel.ItemsSource = images;
            FavPopupDescription.Text = poi.CurrentDescription ?? fav.PoiDescription ?? "";
        }
    }

    // ── Nút Nghe / Dừng ─────────────────────────────────────────

    private async void OnFavPopupPlayClicked(object sender, EventArgs e)
    {
        if (_selectedFav == null) return;

        if (_speechCts != null) // đang đọc → dừng
        {
            StopSpeech();
            FavPlayStopButton.Source = "play_icon.png";
        }
        else // chưa đọc → bắt đầu
        {
            FavPlayStopButton.Source = "stop_icon.png";
            var text = FavPopupDescription.Text;
            await SpeakAsync(text);
            FavPlayStopButton.Source = "play_icon.png";
        }
    }

    // ── Nút Bản đồ ──────────────────────────────────────────────

    private async void OnFavPopupMapClicked(object sender, EventArgs e)
    {
        if (_selectedFav == null) return;

        FavPopupOverlay.IsVisible = false;
        StopSpeech();

        Preferences.Set("MapTargetLat", _selectedFav.Latitude);
        Preferences.Set("MapTargetLon", _selectedFav.Longitude);
        Preferences.Set("MapTargetName", _selectedFav.PoiName);

        await Shell.Current.GoToAsync("//MainPage");
    }

    // ── Nút Đóng ────────────────────────────────────────────────

    private void OnFavPopupCloseClicked(object sender, EventArgs e)
    {
        FavPopupOverlay.IsVisible = false;
        StopSpeech();
        FavPlayStopButton.Source = "play_icon.png";
    }

    // ── Bỏ lưu ──────────────────────────────────────────────────

    private async void OnRemoveFavoriteTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not FavoriteEntity item) return;

        _removeTapping = true;
        try
        {
            bool confirm = await DisplayAlert(
                "Bỏ yêu thích",
                $"Bỏ \"{item.PoiName}\" khỏi danh sách?",
                "Bỏ lưu", "Hủy");

            if (!confirm) return;

            var email = Preferences.Get("userEmail", "");
            await _dbService.RemoveFavoriteAsync(email, item.PoiName);
            await LoadFavorites();
        }
        finally
        {
            _removeTapping = false;
        }
    }

    // ── TTS helpers ──────────────────────────────────────────────

    private async Task SpeakAsync(string text)
    {
        StopSpeech();
        _speechCts = new CancellationTokenSource();
        try
        {
            var locales = await TextToSpeech.Default.GetLocalesAsync();
            string savedLang = Preferences.Get("VoiceLanguage", "vi");
            var voice = locales.FirstOrDefault(l => l.Language.StartsWith(savedLang));

            await TextToSpeech.Default.SpeakAsync(text, new SpeechOptions
            {
                Locale = voice,
                Volume = 1.0f,
                Pitch = 1.0f
            }, _speechCts.Token);
        }
        catch (OperationCanceledException) { }
    }

    private void StopSpeech()
    {
        _speechCts?.Cancel();
        _speechCts?.Dispose();
        _speechCts = null;
    }
}