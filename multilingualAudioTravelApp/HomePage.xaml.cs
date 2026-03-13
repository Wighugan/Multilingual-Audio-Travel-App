using multilingualAudioTravelApp.Services;

namespace multilingualAudioTravelApp;

public class PoiCardItem
{
    public string Name { get; set; }
    public string Image { get; set; }
    public string Description { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string ShortDescription => Description?.Length > 50
        ? Description.Substring(0, 50) + "..."
        : Description;
}

public partial class HomePage : ContentPage
{
    private readonly DatabaseService _dbService = new DatabaseService();
    private List<PoiCardItem> _allPois = new();
    private bool _isSearching = false;
    private PoiCardItem _selectedPoi;
    private CancellationTokenSource _speechCts;

    public HomePage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadPois();
    }

    private async Task LoadPois()
    {
        var entities = await _dbService.GetAllPoisAsync();

        _allPois = entities.Select(e => new PoiCardItem
        {
            Name = e.CurrentName,
            Image = e.Image,
            Description = e.CurrentDescription,
            Latitude = e.Latitude,
            Longitude = e.Longitude
        }).ToList();

        PoiCollection.ItemsSource = _allPois;
    }

    // ── Tìm kiếm ──
    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isSearching && string.IsNullOrEmpty(e.NewTextValue))
        {
            _isSearching = false;
            PoiCollection.ItemsSource = _allPois;
        }
    }

    private void OnSearchButtonPressed(object sender, EventArgs e)
    {
        var keyword = SearchBar.Text?.Trim().ToLower();

        if (string.IsNullOrEmpty(keyword))
        {
            _isSearching = false;
            PoiCollection.ItemsSource = _allPois;
            return;
        }

        _isSearching = true;
        PoiCollection.ItemsSource = _allPois
            .Where(p => p.Name.ToLower().Contains(keyword)
                     || p.Description.ToLower().Contains(keyword))
            .ToList();
    }

    // ── Bấm vào quán → mở popup ──
    private void OnPoiTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not PoiCardItem selected) return;

        _selectedPoi = selected;
        PopupImage.Source = selected.Image;
        PopupTitle.Text = selected.Name;
        PopupDescription.Text = selected.Description;
        PopupOverlay.IsVisible = true;
    }

    private async void OnPopupPlayClicked(object sender, EventArgs e)
    {
        if (_selectedPoi == null) return;

        if (_speechCts != null) // đang đọc → dừng
        {
            OnPopupStopClicked(sender, e);
            PlayStopButton.Source = "play_icon.png";
        }
        else // chưa đọc → bắt đầu
        {
            PlayStopButton.Source = "stop_icon.png";
            await SpeakAsync(_selectedPoi.Description);
            PlayStopButton.Source = "play_icon.png"; // reset sau khi đọc xong
        }
    }

    // ── Nút Dừng ──
    private void OnPopupStopClicked(object sender, EventArgs e)
    {
        _speechCts?.Cancel();
        _speechCts?.Dispose();
        _speechCts = null;
    }

    // ── Nút Xem bản đồ → chuyển sang MainPage ──
    private async void OnPopupMapClicked(object sender, EventArgs e)
    {
        PopupOverlay.IsVisible = false;
        OnPopupStopClicked(sender, e);

        // Lưu tọa độ quán được chọn để MainPage zoom vào
        Preferences.Set("MapTargetLat", _selectedPoi.Latitude);
        Preferences.Set("MapTargetLon", _selectedPoi.Longitude);
        Preferences.Set("MapTargetName", _selectedPoi.Name);

        await Shell.Current.GoToAsync("//MainPage");
    }

    // ── Nút Đóng popup ──
    private void OnPopupCloseClicked(object sender, EventArgs e)
    {
        PopupOverlay.IsVisible = false;
        OnPopupStopClicked(sender, e);
    }

    private async Task SpeakAsync(string text)
    {
        OnPopupStopClicked(null, null); // dừng nếu đang đọc

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
}