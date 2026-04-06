using Microsoft.Maui.Controls;
using multilingualAudioTravelApp.Services;
using ZXing.Net.Maui;

namespace multilingualAudioTravelApp;

public partial class QRScanPage : ContentPage
{
    private readonly DatabaseService _dbService = new DatabaseService();
    private bool _isProcessing = false;
    private string _currentDescription = string.Empty;
    private CancellationTokenSource? _speechCts;
    private bool _isPlaying = false;

    public QRScanPage()
    {
        InitializeComponent();
        // KHÔNG set Options ở đây nữa — chuyển sang OnAppearing
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // ── FIX 1: Set Options SAU khi trang đã render ──
        BarcodeReader.Options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormat.QrCode,
            AutoRotate = true,
            Multiple = false,
            TryHarder = true   // quan trọng cho điện thoại thật
        };

        // ── FIX 2: Xin quyền runtime đúng cách ──
        var status = await Permissions.RequestAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            await DisplayAlert("Cần quyền Camera",
                "Vui lòng vào Cài đặt → Ứng dụng → cấp quyền Camera", "OK");
            StatusLabel.Text = "❌ Chưa có quyền camera";
            StatusLabel.TextColor = Colors.Red;
            return;
        }

        // ── FIX 3: Đảm bảo DB init xong TRƯỚC khi bật camera ──
        await _dbService.InitAsync();

        _isProcessing = false;
        ResultPopupOverlay.IsVisible = false;

        // Bật camera SAU khi mọi thứ sẵn sàng
        BarcodeReader.IsDetecting = true;
        StatusLabel.Text = "Hướng camera vào mã QR";
        StatusLabel.TextColor = Colors.White;

        System.Diagnostics.Debug.WriteLine("[QR] Camera đã bật, sẵn sàng quét");
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        BarcodeReader.IsDetecting = false;
        StopSpeech();
        ResetPlayButton();
    }

    private void OnBarcodeDetected(object sender, BarcodeDetectionEventArgs e)
    {
        // 1. Chặn ngay lập tức ở luồng nền để tránh quét trùng
        if (_isProcessing) return;
        _isProcessing = true;

        var result = e.Results.FirstOrDefault();
        if (result == null || string.IsNullOrWhiteSpace(result.Value))
        {
            _isProcessing = false; // Reset nếu kết quả rỗng
            return;
        }

        // 2. Chỉ chuyển vào MainThread để xử lý giao diện
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            // Tắt bộ quét để tránh quét tiếp trong khi đang hiện thông báo
            BarcodeReader.IsDetecting = false;

            // Gọi hàm xử lý (đã lược bỏ GPS theo yêu cầu trước của bạn)
            await HandleQRCode(result.Value);
        });
    }

    private async Task ProcessDetectedAsync(string qrRawValue)
    {
        try
        {
            await HandleQRCode(qrRawValue);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[QR] Lỗi: {ex}");
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                StatusLabel.Text = "❌ Lỗi xử lý QR";
                StatusLabel.TextColor = Colors.Red;
                await DisplayAlert("Lỗi", $"Lỗi: {ex.Message}", "OK");
            });
            ResetScanner();
        }
    }

    private async Task HandleQRCode(string qrValue)
    {
        System.Diagnostics.Debug.WriteLine($"[QR] Đọc được: '{qrValue}'");
        var poiName = TryExtractPoiName(qrValue);

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            StatusLabel.Text = $"✅ Đọc được: {NormalizeText(qrValue)}";
            StatusLabel.TextColor = Color.FromArgb("#4CAF50");
        });

        if (string.IsNullOrWhiteSpace(poiName))
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                StatusLabel.Text = "❌ QR không hợp lệ";
                StatusLabel.TextColor = Colors.Red;
                await DisplayAlert("Không hợp lệ",
                    $"QR không thuộc hệ thống Vĩnh Khánh.\n\nĐọc được:\n'{qrValue}'", "OK");
            });
            ResetScanner();
            return;
        }

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            StatusLabel.Text = $"🔍 Tìm: {poiName}...";
            StatusLabel.TextColor = Colors.White;
        });

        var allPois = await _dbService.GetAllPoisAsync();

        // Debug in ra DB để kiểm tra
        System.Diagnostics.Debug.WriteLine($"[QR] Tổng số quán trong DB: {allPois.Count}");
        foreach (var p in allPois)
            System.Diagnostics.Debug.WriteLine($"[DB] '{p.CurrentName}'");

        var poi = allPois.FirstOrDefault(p =>
            NormalizeText(p.CurrentName).Equals(poiName,
            StringComparison.OrdinalIgnoreCase));

        if (poi == null)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                StatusLabel.Text = "❌ Không tìm thấy quán";
                StatusLabel.TextColor = Colors.Red;
                await DisplayAlert("Không tìm thấy",
                    $"Không có quán:\n'{poiName}'\n\nTên phải khớp với DB.", "OK");
            });
            ResetScanner();
            return;
        }

        // Tìm thấy
        System.Diagnostics.Debug.WriteLine($"[QR] Tìm thấy: '{poi.CurrentName}'");
        try { HapticFeedback.Perform(HapticFeedbackType.Click); } catch { }

        _currentDescription = poi.CurrentDescription ?? string.Empty;
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            StatusLabel.Text = $"🍜 {poi.CurrentName}";
            StatusLabel.TextColor = Color.FromArgb("#FFC107");
            ResultPopupTitle.Text = "🍜 " + poi.CurrentName;
            ResultPopupDescription.Text = _currentDescription;
            ResultPopupOverlay.IsVisible = true;
        });

        // Lưu để MainPage zoom nếu cần
        Preferences.Set("MapTargetLat", poi.Latitude);
        Preferences.Set("MapTargetLon", poi.Longitude);
        Preferences.Set("MapTargetName", poi.CurrentName);
    }

    private void ResetScanner()
    {
        _isProcessing = false;
        BarcodeReader.IsDetecting = true;
        StatusLabel.Text = "Hướng camera vào mã QR";
        StatusLabel.TextColor = Colors.White;
    }

    private async Task SpeakAsync(string text)
    {
        try
        {
            StopSpeech();
            _speechCts = new CancellationTokenSource();
            _isPlaying = true;

            var locales = await TextToSpeech.Default.GetLocalesAsync();
            string lang = Preferences.Get("AppLanguage", "vi");
            var voice = locales.FirstOrDefault(l =>
                l.Language.StartsWith(lang, StringComparison.OrdinalIgnoreCase));

            await TextToSpeech.Default.SpeakAsync(text, new SpeechOptions
            {
                Locale = voice,
                Volume = 1.0f,
                Pitch = 1.0f
            }, _speechCts.Token);
        }
        catch (OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine("[TTS] Đã dừng.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TTS] Lỗi: {ex.Message}");
        }
        finally
        {
            _isPlaying = false;
            ResetPlayButton();
        }
    }

    private static string NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        return value.Trim()
                    .Replace("\r", string.Empty)
                    .Replace("\n", string.Empty);
    }

    private static string TryExtractPoiName(string raw)
    {
        var normalized = NormalizeText(raw);
        if (string.IsNullOrWhiteSpace(normalized))
            return string.Empty;

        if (normalized.StartsWith("POI:", StringComparison.OrdinalIgnoreCase))
            return NormalizeText(normalized.Substring(4));

        // Hỗ trợ mã QR chỉ chứa tên quán
        return normalized;
    }

    private async void OnCloseClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async void OnResultPlayClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_currentDescription)) return;

        if (_isPlaying)
        {
            StopSpeech();
            return;
        }

        ResultPlayStopButton.Source = "stop_icon.png";
        await SpeakAsync(_currentDescription);
    }

    private void OnResultCloseClicked(object sender, EventArgs e)
    {
        StopSpeech();
        _isPlaying = false;
        ResetPlayButton();
        ResultPopupOverlay.IsVisible = false;
        ResetScanner();
    }

    private void StopSpeech()
    {
        if (_speechCts == null) return;
        try { _speechCts.Cancel(); _speechCts.Dispose(); }
        catch { }
        finally { _speechCts = null; _isPlaying = false; ResetPlayButton(); }
    }

    private void ResetPlayButton()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ResultPlayStopButton.Source = "play_icon.png";
        });
    }
}