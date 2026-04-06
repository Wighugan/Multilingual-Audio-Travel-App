using Microsoft.Maui.Devices.Sensors;
using multilingualAudioTravelApp.Services;
using Plugin.LocalNotification;


namespace multilingualAudioTravelApp;

public partial class ExplorePage : ContentPage
{
    IDispatcherTimer _gpsTimer;
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
            PopupTitle.Text = _selectedPoi.CurrentName;
            PopupDescription.Text = _selectedPoi.CurrentDescription;
            PopupImage.Source = _selectedPoi.Image;
            PopupOverlay.IsVisible = true;
        }
    }

    private void OnPopupCloseClicked(object sender, EventArgs e)
    {
        PopupOverlay.IsVisible = false;
        StopSpeech();
        ResetAudioState();
    }

    private async void OnPopupPlayClicked(object sender, EventArgs e)
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
            await SpeakDescription(_selectedPoi.CurrentDescription);
        }
    }

    private async void OnPopupMapClicked(object sender, EventArgs e)
    {
        PopupOverlay.IsVisible = false;
        StopSpeech();
        ResetAudioState();
        await Shell.Current.GoToAsync("//MainPage");
    }

    /*   private async Task SpeakDescription(string text)
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
       }*/
    private async Task SpeakDescription(string text)
    {
        StopSpeech();
        if (string.IsNullOrWhiteSpace(text)) return;

        _speechCts = new CancellationTokenSource();
        _isPlaying = true;

        try
        {
            // 1. Lấy danh sách tất cả các giọng đọc có sẵn trên điện thoại
            var locales = await TextToSpeech.Default.GetLocalesAsync();

            // 2. Lấy ngôn ngữ người dùng đang chọn ("vi" hoặc "en")
            string savedLang = Preferences.Get("AppLanguage", "vi");

            // 3. Tìm đúng giọng đọc khớp với ngôn ngữ
            // - Nếu savedLang = "vi", tìm giọng có mã bắt đầu bằng "vi" (Ví dụ: vi-VN)
            // - Nếu savedLang = "en", tìm giọng có mã bắt đầu bằng "en" (Ví dụ: en-US hoặc en-GB)
            var voice = locales.FirstOrDefault(l => l.Language.StartsWith(savedLang, StringComparison.OrdinalIgnoreCase));

            var options = new SpeechOptions
            {
                Locale = voice, // Gắn giọng đọc chuẩn vào đây
                Volume = 1.0f,
                Pitch = 1.0f
            };

            // Bắt đầu đọc
            await TextToSpeech.Default.SpeakAsync(text, options, _speechCts.Token);

            ResetAudioState();
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