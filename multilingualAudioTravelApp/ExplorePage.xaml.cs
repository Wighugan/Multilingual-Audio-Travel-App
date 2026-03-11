using Microsoft.Maui.Devices.Sensors;
using multilingualAudioTravelApp.Services;

namespace multilingualAudioTravelApp;

public partial class ExplorePage : ContentPage
{
    private readonly DatabaseService _dbService = new DatabaseService();
    private PoiEntity _selectedPoi;
    private CancellationTokenSource _speechCts;
    private bool _isPlaying = false;

    public ExplorePage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        var entities = await _dbService.GetAllPoisAsync();
        PoiCollectionView.ItemsSource = entities;
    }
    /*private void OnPoiSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is PoiEntity selectedItem)
        {
            _selectedPoi = selectedItem;

            PopupTitle.Text = _selectedPoi.Name;
            PopupDescription.Text = _selectedPoi.Description;
            PopupImage.Source = _selectedPoi.Image;

            PoiCollectionView.SelectedItem = null;

            PopupOverlay.IsVisible = true;
        }
    }*/
    private void OnPoiTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is PoiEntity selectedItem)
        {
            _selectedPoi = selectedItem;
            PopupTitle.Text = _selectedPoi.Name;
            PopupDescription.Text = _selectedPoi.Description;
            PopupImage.Source = _selectedPoi.Image;
            PopupOverlay.IsVisible = true;
        }
    }

    private void OnClosePopup(object sender, EventArgs e)
    {
        PopupOverlay.IsVisible = false;
        StopSpeech();
        ResetAudioState();
    }

    private async void OnPlayStopClicked(object sender, EventArgs e)
    {
        if (_selectedPoi == null) return;

        if (_isPlaying)
        {
            StopSpeech();
            ResetAudioState();
        }
        else
        {
            PlayStopButton.Source = "stop_icon.png";
            await SpeakDescription(_selectedPoi.Description);
        }
    }

    private async Task SpeakDescription(string text)
    {
        StopSpeech();

        if (string.IsNullOrWhiteSpace(text)) return;

        _speechCts = new CancellationTokenSource();
        _isPlaying = true;

        try
        {
            var locales = await TextToSpeech.Default.GetLocalesAsync();
            string savedLang = Preferences.Get("AppLanguage", "vi");
            var voice = locales.FirstOrDefault(l => l.Language.StartsWith(savedLang));

            var options = new SpeechOptions
            {
                Locale = voice,
                Volume = 1.0f,
                Pitch = 1.0f
            };

            await TextToSpeech.Default.SpeakAsync(text, options, _speechCts.Token);

            ResetAudioState();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Lỗi đọc TTS: {ex.Message}");
            ResetAudioState();
        }
    }

    private void StopSpeech()
    {
        if (_speechCts != null)
        {
            _speechCts.Cancel();
            _speechCts.Dispose();
            _speechCts = null;
        }
    }

    private void ResetAudioState()
    {
        _isPlaying = false;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            PlayStopButton.Source = "play_icon.png";
        });
    }

    private void OnSearchClicked(object sender, EventArgs e)
    {
        DisplayAlert("Search", "Tìm kiếm", "OK");
    }

    private void OnNotifyClicked(object sender, EventArgs e)
    {
        DisplayAlert("Thông báo", "Chưa có thông báo", "OK");
    }
}