using Mapsui; 
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Limiting;
using Mapsui.Nts;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.UI.Maui;
using Microsoft.Maui.Devices.Sensors;
using MBrush = Mapsui.Styles.Brush;
using MColor = Mapsui.Styles.Color;
using SkiaSharp;
using multilingualAudioTravelApp.Services;


namespace multilingualAudioTravelApp;

public partial class MainPage : ContentPage
{
    private MemoryLayer _currentLocationLayer;
    private MemoryLayer _poiLayer;
    private bool _isTracking = false;
    private Location _currentLocation;
    private CancellationTokenSource _speechCts;
    private DateTime _lastGeofenceCheckTime = DateTime.MinValue;
    private List<PoiData> _poiList = new List<PoiData>();
    private PoiData _selectedPoi;
    private readonly DatabaseService _dbService = new DatabaseService();

    // ===== AUDIO CONTROL =====
    // ===== AUDIO CONTROL =====
    private string _currentText;
    private string[] _sentences;
    private int _currentSentenceIndex = 0;
    private bool _isPaused = false;
    private bool _isPlaying = false;
    public MainPage()
    {
        InitializeComponent();
        InitializeMap();
    }

    private void InitializeMap()
    {
        MyMap.Map.Layers.Add(OpenStreetMap.CreateTileLayer());
        
        foreach (var widget in MyMap.Map.Widgets)
        {
            string widgetName = widget.GetType().Name;

            if (widgetName == "LoggingWidget" || widgetName == "PerformanceWidget")
            {
                widget.Enabled = false;
            }
        }

        _currentLocationLayer = new MemoryLayer
        {
            Name = "LocationLayer",
            Style = null // style riêng từng feature
        };
        MyMap.Map.Layers.Add(_currentLocationLayer);

        Dispatcher.Dispatch(async () =>
        {
            await CreatePoiLayer();
        });

        MyMap.Info += OnMapInfo;

        Dispatcher.Dispatch(async () =>
        {
            try
            {
                var vinhKhanhCenter = SphericalMercator.FromLonLat(106.7035, 10.7605);
                var centerPoint = new MPoint(vinhKhanhCenter.x, vinhKhanhCenter.y);
                MyMap.Map.Navigator.CenterOn(centerPoint);
                MyMap.Map.Navigator.ZoomTo(1);
                await Task.Delay(500);
                try {
                    var min = SphericalMercator.FromLonLat(106.7010, 10.7570);
                    var max = SphericalMercator.FromLonLat(106.7060, 10.7640);
                    //MyMap.Map.Navigator.OverridePanBounds = new MRect(min.x, min.y, max.x, max.y);
                  //  MyMap.Map.Navigator.OverrideZoomBounds = new MMinMax(0.1, 5);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Lỗi khóa map: " + ex.Message);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        });
    }




    public class PoiData
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Image { get; set; }   // thêm dòng này
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime LastPlayedTime { get; set; } = DateTime.MinValue;
        public TimeSpan CooldownDuration { get; set; } = TimeSpan.FromMinutes(5);
        public double Radius { get; set; } = 50;
        public int Priority { get; set; } = 1;
    }

    private async Task CreatePoiLayer()
    {
        var entities = await _dbService.GetAllPoisAsync();

        _poiList = entities.Select(e => new PoiData
        {
            Name = e.CurrentName,
            Description = e.CurrentDescription,
            Image = e.Image,
            Latitude = e.Latitude,
            Longitude = e.Longitude,
            Radius = e.Radius,
            Priority = e.Priority,
            CooldownDuration = TimeSpan.FromMinutes(e.CooldownMinutes)
        }).ToList();

        var features = new List<IFeature>();

        foreach (var poi in _poiList)
        {
            var coords = SphericalMercator.FromLonLat(poi.Longitude, poi.Latitude);

            var feature = new PointFeature(new MPoint(coords.x, coords.y));

            feature["Name"] = poi.Name;

            feature.Styles.Add(new ImageStyle
            {
                Image = new Mapsui.Styles.Image
                {
                    Source = "svg-content://<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"36\" height=\"56\"><path d=\"M18 .34C8.325.34.5 8.168.5 17.81c0 3.339.962 6.441 2.594 9.094H3l7.82 15.117L18 55.903l7.187-13.895L33 26.903h-.063c1.632-2.653 2.594-5.755 2.594-9.094C35.531 8.169 27.675.34 18 .34zm0 9.438a6.5 6.5 0 1 1 0 13 6.5 6.5 0 0 1 0-13z\" fill=\"#E53935\" stroke=\"#B71C1C\" stroke-width=\"1\"/></svg>"
                },
                SymbolScale = 1,
                Offset = new Offset(0, 28)
            });

            features.Add(feature);
        }

        _poiLayer = new MemoryLayer
        {
            Name = "PoiLayer",
            Features = features,
            Style = null
        };

        MyMap.Map.Layers.Add(_poiLayer);
    }

    public async Task RefreshPoiLayerAsync()
    {
        var oldLayer = MyMap.Map.Layers.FirstOrDefault(l => l.Name == "PoiLayer");

        if (oldLayer != null)
            MyMap.Map.Layers.Remove(oldLayer);

        await CreatePoiLayer();

        MyMap.Refresh();
    }

    // Hàm xử lý khi người dùng chạm vào bản đồ
    private void OnMapInfo(object sender, MapInfoEventArgs e)
    {
        var mapInfo = e.GetMapInfo(new List<ILayer> { _poiLayer });

        if (mapInfo?.Feature != null)
        {
            var name = mapInfo.Feature["Name"]?.ToString();

            _selectedPoi = _poiList.FirstOrDefault(p => p.Name == name);

            if (_selectedPoi != null)
            {
                PopupTitle.Text = _selectedPoi.Name;
                PopupDescription.Text = _selectedPoi.Description;
                PopupImage.Source = _selectedPoi.Image;
                PopupOverlay.IsVisible = true;
            }
        }
    }


    private void OnPopupCloseClicked(object sender, EventArgs e)
    {
        PopupOverlay.IsVisible = false;
        StopSpeech();
        ResetAudioState();
    }

    private async void OnSpeakClicked(object sender, EventArgs e)
    {
        if (_selectedPoi != null)
        {
            await SpeakDescription(_selectedPoi.Description);
        }
    }
    /*private async Task SpeakDescription(string text, bool resume = false)
    {
        if (!resume)
        {
            StopSpeech();

            _currentText = text;
            _sentences = text.Split('.', StringSplitOptions.RemoveEmptyEntries);
            _currentSentenceIndex = 0;
            _isPaused = false;
        }

        if (_sentences == null || _sentences.Length == 0)
            return;

        _speechCts = new CancellationTokenSource();
        _isPlaying = true;

        try
        {
            var locales = await TextToSpeech.Default.GetLocalesAsync();
            string savedLang = Preferences.Get("VoiceLanguage", "vi");
            var voice = locales.FirstOrDefault(l => l.Language.StartsWith(savedLang));

            var options = new SpeechOptions
            {
                Locale = voice,
                Volume = 1.0f,
                Pitch = 1.0f
            };

            for (int i = _currentSentenceIndex; i < _sentences.Length; i++)
            {
                _currentSentenceIndex = i;

                if (string.IsNullOrWhiteSpace(_sentences[i]))
                    continue;

                await TextToSpeech.Default.SpeakAsync(
                    _sentences[i],
                    options,
                    _speechCts.Token);
            }

            // Đọc xong hoàn toàn
            ResetAudioState();
        }
        catch (OperationCanceledException)
        {
            _isPaused = true;
            _isPlaying = false;
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

            await SpeakDescription(_selectedPoi.Description);
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
        _currentSentenceIndex = 0;
        _isPaused = false;
        _isPlaying = false;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Trả icon về lại nút Play khi đã đọc xong hoặc bị dừng
            PlayStopButton.Source = "play_icon.png";
        });
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await StartListeningGps();

        // Refresh POI để lấy data mới nhất từ DB
        await RefreshPoiLayerAsync();

        // Kiểm tra có quán được chọn từ HomePage không
        var lat = Preferences.Get("MapTargetLat", 0.0);
        var lon = Preferences.Get("MapTargetLon", 0.0);

        if (lat != 0 && lon != 0)
        {
            Preferences.Remove("MapTargetLat");
            Preferences.Remove("MapTargetLon");
            Preferences.Remove("MapTargetName");

            var coords = SphericalMercator.FromLonLat(lon, lat);
            var point = new MPoint(coords.x, coords.y);
            MyMap.Map.Navigator.CenterOn(point);
            MyMap.Map.Navigator.ZoomTo(5);
        }
    }

    // Khi thoát màn hình hoặc ẩn app thì dừng theo dõi
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopListeningGps();
    }

    // Hàm bắt đầu lắng nghe
    private async Task StartListeningGps()
    {
        if (_isTracking) return;

        var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted) return;
        }

        // Đăng ký sự kiện: Khi vị trí thay đổi thì gọi hàm OnLocationChanged
        Geolocation.LocationChanged += OnLocationChanged;

        // Cấu hình: Cập nhật mỗi 2 giây hoặc khi đi được 5 mét
        var request = new GeolocationListeningRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(2));

        // Bắt đầu lắng nghe
        await Geolocation.StartListeningForegroundAsync(request);
        _isTracking = true;
    }

    private void StopListeningGps()
    {
        if (!_isTracking) return;

        Geolocation.LocationChanged -= OnLocationChanged;
        Geolocation.StopListeningForeground();
        _isTracking = false;
    }

    private void OnLocationChanged(object sender, GeolocationLocationChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateUserLocationOnMap(e.Location);
            if ((DateTime.Now - _lastGeofenceCheckTime).TotalSeconds < 3) //check geofence mỗi 3 giây
            {
                return;
            }
            _lastGeofenceCheckTime = DateTime.Now;
            CheckGeofence(e.Location);
        });
    }

    private void UpdateUserLocationOnMap(Location location)
    {
        if (location == null) return;

        // Lưu lại vị trí hiện tại để nút định vị dùng
        _currentLocation = location;

        try
        {
            var smPoint = SphericalMercator.FromLonLat(location.Longitude, location.Latitude);
            var mapPoint = new MPoint(smPoint.x, smPoint.y);

            var feature = new PointFeature(mapPoint);

            // Vòng sáng xanh bên ngoài (hiệu ứng accuracy)
            feature.Styles.Add(new SymbolStyle
            {
                SymbolType = SymbolType.Ellipse,
                Fill = new MBrush(new MColor(66, 133, 244, 50)), // xanh trong suốt
                Outline = new Pen(new MColor(66, 133, 244, 80), 1),
                SymbolScale = 2.5,
            });

            // Viền trắng
            feature.Styles.Add(new SymbolStyle
            {
                SymbolType = SymbolType.Ellipse,
                Fill = new MBrush(MColor.White),
                SymbolScale = 0.7,
            });

            // Chấm xanh chính giữa
            feature.Styles.Add(new SymbolStyle
            {
                SymbolType = SymbolType.Ellipse,
                Fill = new MBrush(new MColor(66, 133, 244)), // xanh Google
                SymbolScale = 0.5,
            });

            _currentLocationLayer.Features = new List<IFeature> { feature };
            _currentLocationLayer.DataHasChanged();

            // Theo dõi vị trí — tự động di chuyển bản đồ theo người dùng
            MyMap.Map.Navigator.CenterOn(mapPoint);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
    private void OnLocateMeTapped(object sender, TappedEventArgs e)
    {
        if (_currentLocation == null)
        {
            DisplayAlert("", "Chưa xác định được vị trí", "OK");
            return;
        }

        var smPoint = SphericalMercator.FromLonLat(
            _currentLocation.Longitude,
            _currentLocation.Latitude);

        var point = new MPoint(smPoint.x, smPoint.y);
        MyMap.Map.Navigator.CenterOn(point);
        MyMap.Map.Navigator.ZoomTo(4);
    }
    private void CheckGeofence(Location userLocation)
    {
        var candidates = _poiList
            .Select(p => new
            {
                Poi = p,
                Distance = Location.CalculateDistance(
                    userLocation.Latitude, userLocation.Longitude,
                    p.Latitude, p.Longitude, DistanceUnits.Kilometers) * 1000
            })
            .Where(x => x.Distance <= x.Poi.Radius && (DateTime.Now - x.Poi.LastPlayedTime) > x.Poi.CooldownDuration) //đọc lại sau 5 phút
            .OrderByDescending(x => x.Poi.Priority)
            .ThenBy(x => x.Distance)               
            .ToList();

        var bestMatch = candidates.FirstOrDefault();

        if (bestMatch != null)
        {
            var poi = bestMatch.Poi;

            poi.LastPlayedTime = DateTime.Now;

            // Auto play
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try { HapticFeedback.Perform(HapticFeedbackType.LongPress); } catch { }
                System.Diagnostics.Debug.WriteLine($"---> Tự động đọc: {poi.Name}");
                await SpeakDescription(poi.Description);
            });
        }
    }

}