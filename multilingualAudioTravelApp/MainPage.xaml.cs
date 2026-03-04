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


namespace multilingualAudioTravelApp;

public partial class MainPage : ContentPage
{
    private MemoryLayer _currentLocationLayer;
    private MemoryLayer _poiLayer;
    private bool _isTracking = false;
    private CancellationTokenSource _speechCts;
    private DateTime _lastGeofenceCheckTime = DateTime.MinValue;
    private List<PoiData> _poiList = new List<PoiData>();
    private PoiData _selectedPoi;
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
            Style = new SymbolStyle
            {
                Fill = new MBrush(MColor.Red),
                SymbolScale = 0.5,
                Outline = new Pen { Color = MColor.White, Width = 2 }
            }
        };
        MyMap.Map.Layers.Add(_currentLocationLayer);
        CreatePoiLayer();
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
                    MyMap.Map.Navigator.OverridePanBounds = new MRect(min.x, min.y, max.x, max.y);
                    MyMap.Map.Navigator.OverrideZoomBounds = new MMinMax(0.1, 5);
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

    private void CreatePoiLayer()
    {
        _poiLayer = new MemoryLayer
        {
            Name = "PoiLayer",
            
        };

        _poiList = new List<PoiData>
    {
        new PoiData
        {
            Name = "Khu phố ẩm thực Vĩnh Khánh",
            Description = "Phố ẩm thực nổi tiếng quận 4 với rất nhiều món ngon hấp dẫn.",
            Image = "vinhkhanh.jpg",
            Latitude = 10.761923,
             Longitude = 106.701964,
            Radius = 100,
            Priority = 10
        },

        new PoiData
        {
            Name = "Quán Ốc Oanh",
            Description = "Quán ốc lâu đời và nổi tiếng nhất khu Vĩnh Khánh.",
            Image = "ocoanh.jpg",
            Latitude = 10.761411,
            Longitude = 106.702734,
            Radius = 80,
            Priority = 8
        },

        new PoiData
        {
            Name = "Quán Ốc Phát",
            Description = "Ốc Phát Vĩnh Khánh vẫn luôn là điểm đến quen thuộc cho các tín đồ mê ốc. Với không gian thoáng đáng, quán Ốc Phát Vĩnh Khánh đích thị là một điểm hẹn hò lý tưởng.",
            Image = "bunca.jpg",
            Latitude = 10.761921,
            Longitude = 106.702121,
            Radius = 70,
            Priority = 6
        }
    };

        var features = new List<IFeature>();

        foreach (var poi in _poiList)
        {
            var coords = SphericalMercator.FromLonLat(poi.Longitude, poi.Latitude);
            var feature = new PointFeature(new MPoint(coords.x, coords.y));

            feature["Name"] = poi.Name;

            feature.Styles.Add(new SymbolStyle
            {
                // BitmapId = BitmapRegistry.Instance.Register(
                //     typeof(MainPage).Assembly.GetManifestResourceStream(
                //         "multilingualAudioTravelApp.Resources.Images.map.png")),
                SymbolType = SymbolType.Ellipse, // Use a built-in symbol type
                Fill = new MBrush(MColor.Red),
                SymbolScale = 0.6
            });

            features.Add(feature);
        }

        _poiLayer.Features = features;
        _poiLayer.DataHasChanged();   
        MyMap.Map.Layers.Add(_poiLayer);
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


    private void OnClosePopup(object sender, EventArgs e)
    {
        PopupOverlay.IsVisible = false;
    }

    private async void OnSpeakClicked(object sender, EventArgs e)
    {
        if (_selectedPoi != null)
        {
            await SpeakDescription(_selectedPoi.Description);
        }
    }
    private async Task SpeakDescription(string text, bool resume = false)
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
    }

    private async void OnPlayClicked(object sender, EventArgs e)
    {
        if (_selectedPoi != null)
            await SpeakDescription(_selectedPoi.Description);
    }

    private void OnPauseClicked(object sender, EventArgs e)
    {
        if (_isPlaying)
        {
            _speechCts?.Cancel();
            _isPaused = true;
            _isPlaying = false;
        }
    }

    private async void OnResumeClicked(object sender, EventArgs e)
    {
        if (_isPaused && _currentText != null)
        {
            _isPaused = false;
            await SpeakDescription(_currentText, true);
        }
    }

    private void OnStopClicked(object sender, EventArgs e)
    {
        StopSpeech();
        ResetAudioState();
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
    }


    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await StartListeningGps();
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

        try
        {
            var smPoint = SphericalMercator.FromLonLat(location.Longitude, location.Latitude);
            var mapPoint = new MPoint(smPoint.x, smPoint.y);

            var pointFeature = new PointFeature(mapPoint);
            var features = new List<IFeature> { pointFeature };

            _currentLocationLayer.Features = features;
            _currentLocationLayer.DataHasChanged();

            MyMap.Map.Navigator.CenterOn(mapPoint);

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message); // Ghi log nếu lỗi
        }
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