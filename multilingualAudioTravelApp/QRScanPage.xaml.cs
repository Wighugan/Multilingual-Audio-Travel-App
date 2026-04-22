using Microsoft.Maui.Controls;
using multilingualAudioTravelApp.Services;
using System.Net.Http.Json;
using ZXing.Net.Maui;

namespace multilingualAudioTravelApp;

public partial class QRScanPage : ContentPage
{
    private readonly DatabaseService _dbService = new DatabaseService();
    private bool _isProcessing = false;
    private string _currentDescription = string.Empty;
    private CancellationTokenSource? _speechCts;
    private bool _isPlaying = false;
    // SỬA DÒNG ĐÓ LẠI THÀNH:
    private readonly HttpClient _httpClient = new HttpClient(); 
    public QRScanPage()
    {
        InitializeComponent();
        _httpClient.BaseAddress = new Uri(DatabaseService.GlobalApiUrl + "/");
        // KHÔNG set Options ở đây nữa — chuyển sang OnAppearing
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // ── FIX 1: Set Options SAU khi trang đã render ──
        BarcodeReader.Options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormat.QrCode,
            AutoRotate = false,
            Multiple = false,
            TryHarder = false   // quan trọng cho điện thoại thật
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
           // BarcodeReader.IsDetecting = false;

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

    private async Task HandleQRCode(string qrToken)
    {
        System.Diagnostics.Debug.WriteLine($"[QR] Đọc được Token: '{qrToken}'");
        qrToken = qrToken.Trim(); // THÊM DÒNG NÀY VÀO TRƯỚC KHI GỌI API

        if (qrToken.StartsWith("VKPREMIUM_", StringComparison.OrdinalIgnoreCase))
        {
            await HandlePremiumQR(qrToken);
            return;
        }

        // ==========================================
        // THÊM TOÀN BỘ ĐOẠN NÀY ĐỂ CHẶN KHÁCH FREE
        // ==========================================
        string userEmail = Preferences.Get("userEmail", "");
        bool isPremium = Preferences.Get($"IsPremium_{userEmail}", false);

        if (!isPremium) // Nếu KHÔNG phải khách Premium
        {
            string today = DateTime.Now.ToString("yyyyMMdd");
            int scanCount = Preferences.Get($"ScanCount_{today}", 0);

            // Giới hạn 3 lần 1 ngày
            if (scanCount >= 3)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    StatusLabel.Text = "❌ Hết lượt miễn phí";
                    StatusLabel.TextColor = Colors.Red;

                    bool upgrade = await DisplayAlert("Đã hết lượt",
                        "Bạn đã dùng hết 3 lượt quét miễn phí hôm nay. Hãy nâng cấp Premium để nghe không giới hạn nhé!",
                        "Mua Premium", "Để sau");

                    // Nếu khách bấm "Mua Premium", mở trang PremiumPage lên
                    if (upgrade)
                    {
                        await Navigation.PushAsync(new PremiumPage());
                    }
                });
                ResetScanner();
                return; // Dừng lại, không cho quét quán này
            }

            // Nếu chưa hết lượt, cộng thêm 1 lần quét cho ngày hôm nay
            Preferences.Set($"ScanCount_{today}", scanCount + 1);
        }
        // ==========================================


        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            StatusLabel.Text = $"🔍 Đang kiểm tra mã trên máy chủ...";
            StatusLabel.TextColor = Colors.White;
        });

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            StatusLabel.Text = $"🔍 Đang kiểm tra mã trên máy chủ...";
            StatusLabel.TextColor = Colors.White;
        });

        try
        {
            // Gọi API xác thực mã (GET /api/pois/verify/{token})
            var response = await _httpClient.GetAsync($"api/pois/verify/{qrToken}");

            if (!response.IsSuccessStatusCode)
            {
                // Token không tồn tại -> Thông báo lỗi
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    StatusLabel.Text = "❌ Mã QR không tồn tại";
                    StatusLabel.TextColor = Colors.Red;
                    await DisplayAlert("Lỗi", "Mã QR này không tồn tại trong hệ thống.", "OK");
                });
                ResetScanner();
                return;
            }

            // Token hợp lệ -> Lấy toàn bộ thông tin POI
            var poi = await response.Content.ReadFromJsonAsync<PoiEntity>();

            try { HapticFeedback.Perform(HapticFeedbackType.Click); } catch { }

            _currentDescription = poi.CurrentDescription ?? string.Empty;

            // Hiển thị Popup chi tiết Quán
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                StatusLabel.Text = $"✅ {poi.CurrentName}";
                StatusLabel.TextColor = Color.FromArgb("#4CAF50");

                ResultPopupTitle.Text = "🍜 " + poi.CurrentName;
                ResultPopupDescription.Text = _currentDescription;
                ResultPopupOverlay.IsVisible = true;

                // Đổi icon nút play thành stop
                ResultPlayStopButton.Source = "stop_icon.png";
            });

            // Lưu tọa độ để khi đóng popup, bản đồ sẽ nhảy tới vị trí quán
            Preferences.Set("MapTargetLat", poi.Latitude);
            Preferences.Set("MapTargetLon", poi.Longitude);
            Preferences.Set("MapTargetName", poi.CurrentName);

            //Đẩy Text vào TTS và Phát Audio
            await SpeakAsync(_currentDescription);
        }
        catch (Exception ex)
        {
            // Xử lý trường hợp điện thoại mất mạng hoặc Server sập
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                StatusLabel.Text = "❌ Lỗi kết nối mạng";
                StatusLabel.TextColor = Colors.Red;
                await DisplayAlert("Lỗi Kết Nối", $"Không thể kết nối đến máy chủ. Vui lòng kiểm tra lại mạng.\n({ex.Message})", "OK");
            });
            ResetScanner();
        }
    }

    private async void ResetScanner()
    {
        StatusLabel.Text = "Hướng camera vào mã QR";
        StatusLabel.TextColor = Colors.White;

        // XÓA HOẶC COMMENT DÒNG BẬT CAMERA NÀY:
        // BarcodeReader.IsDetecting = true;

        // Đợi 1.5 giây rồi mới "mở khóa" cho phép quét lần 2
        // Giúp người dùng có thời gian di chuyển điện thoại ra khỏi mã QR cũ
        await Task.Delay(1500);
        _isProcessing = false;
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
    private async Task HandlePremiumQR(string token)
    {
        var parts = token.Split('_');
        var expiry = parts.Length >= 3 ? parts[^1] : "";
        var email = parts.Length >= 3
            ? string.Join("_", parts.Skip(1).Take(parts.Length - 2))
            : "";

        // ── Kiểm tra email trong QR có khớp user đang đăng nhập không ──
        var currentEmail = Preferences.Get("userEmail", "");
        if (!email.Equals(currentEmail, StringComparison.OrdinalIgnoreCase))
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                StatusLabel.Text = "❌ QR không thuộc tài khoản này";
                StatusLabel.TextColor = Colors.Red;
                await DisplayAlert("Không hợp lệ",
                    "Mã QR này không thuộc tài khoản đang đăng nhập.", "OK");
            });
            ResetScanner();
            return;
        }

        // ── Kiểm tra còn hạn ──
        bool isValid = DateTime.TryParse(expiry, out var expDate)
                       && expDate >= DateTime.Today;

        if (!isValid)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                PremiumPopupTitle.Text = "❌ Thẻ Premium đã hết hạn";
                PremiumPopupTitle.TextColor = Colors.Red;
                PremiumEmailLabel.Text = email;
                PremiumExpiryLabel.Text = expDate.ToString("dd/MM/yyyy");
                PremiumStatusLabel.Text = "Hết hạn";
                PremiumStatusLabel.TextColor = Colors.Red;
                StatusLabel.Text = "❌ QR hết hạn";
                StatusLabel.TextColor = Colors.Red;
                PremiumPopupOverlay.IsVisible = true;
            });
            return;
        }

        // Còn hạn: load data TRƯỚC, show UI SAU
        try { HapticFeedback.Perform(HapticFeedbackType.Click); } catch { }

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            StatusLabel.Text = "✅ Premium hợp lệ - Đang tải...";
            StatusLabel.TextColor = Color.FromArgb("#4CAF50");
        });

        // Await data ngoài MainThread
        var allPois = await _dbService.GetAllPoisAsync();

        // Xong mới show popup
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            PremiumPoiList.ItemsSource = allPois;
            PremiumListOverlay.IsVisible = true;
        });
    }

    private PoiEntity _selectedPremiumPoi;

    private void OnPremiumPoiTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not PoiEntity poi) return;
        _selectedPremiumPoi = poi;

        PremiumSpeakTitle.Text = poi.CurrentName;
        PremiumSpeakDesc.Text = poi.CurrentDescription;
        PremiumPlayBtn.Source = "play_icon.png";
        StopSpeech();

        PremiumSpeakOverlay.IsVisible = true;
    }

    private async void OnPremiumPlayClicked(object sender, EventArgs e)
    {
        if (_selectedPremiumPoi == null) return;

        if (_isPlaying)
        {
            StopSpeech();
            PremiumPlayBtn.Source = "play_icon.png";
        }
        else
        {
            PremiumPlayBtn.Source = "stop_icon.png";
            _currentDescription = _selectedPremiumPoi.CurrentDescription;
            await SpeakAsync(_currentDescription);
            PremiumPlayBtn.Source = "play_icon.png";
        }
    }

    private void OnPremiumSpeakCloseClicked(object sender, EventArgs e)
    {
        StopSpeech();
        PremiumPlayBtn.Source = "play_icon.png";
        PremiumSpeakOverlay.IsVisible = false;
    }

    private void OnPremiumListCloseClicked(object sender, EventArgs e)
    {
        StopSpeech();
        PremiumSpeakOverlay.IsVisible = false;
        PremiumListOverlay.IsVisible = false;
        ResetScanner();
    }

    private void OnPremiumPopupCloseClicked(object sender, EventArgs e)
    {
        PremiumPopupOverlay.IsVisible = false;
        ResetScanner();
    }
}