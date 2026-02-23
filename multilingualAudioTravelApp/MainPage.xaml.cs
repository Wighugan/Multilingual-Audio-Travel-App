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
        public double Latitude { get; set; }
        public double Longitude { get; set; }
 //        public bool HasPlayed { get; set; } = false;
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
            Style = new SymbolStyle
            {
                Fill = new MBrush(MColor.Cyan),
                SymbolScale = 0.5,
                Outline = new Pen { Color = MColor.White, Width = 2 }
            }
        };

        var khuPhoAmThuc = new PoiData
        {
            Name = "Khu phố ẩm thực Vĩnh Khánh",
            Description = "Ở quận 4 nhắc đến phố ẩm thực thì Vĩnh Khánh chính là cái tên nổi bật nhất mà bạn chắc chắn phải ghé đến. Tại đây có rất nhiều hàng quán với vô số món ăn hấp dẫn. Bên cạnh đó, phố ẩm thực Vĩnh Khánh cũng khá gần trung tâm, cách Dinh Độc Lập chỉ 2.9km nên rất thuận tiện để bạn ghé đến khi có dịp du lịch Sài Gòn.",
            Latitude = 10.7605,
            Longitude = 106.7035,
            Radius = 100, //100m
            Priority = 10
        };
        _poiList.Add(khuPhoAmThuc);

        var features = new List<IFeature>();

        foreach (var poi in _poiList)
        {
            // Chuyển tọa độ GPS sang tọa độ Bản đồ (SphericalMercator)
            var coords = SphericalMercator.FromLonLat(poi.Longitude, poi.Latitude);
            var feature = new PointFeature(new MPoint(coords.x, coords.y));

            // Gắn dữ liệu text vào để hiện Popup khi bấm
            feature["Name"] = poi.Name;
            feature["Description"] = poi.Description;
            features.Add(feature);
        }

        //Cập nhật vào Layer
        _poiLayer.Features = features;
        MyMap.Map.Layers.Add(_poiLayer);
    }

    // Hàm xử lý khi người dùng chạm vào bản đồ
    private async void OnMapInfo(object sender, MapInfoEventArgs e)
    {
        // Lấy thông tin từ lớp PoiLayer
        var mapInfo = e.GetMapInfo(new List<ILayer> { _poiLayer });

        // Kiểm tra xem có chạm trúng điểm nào không
        if (mapInfo != null && mapInfo.Feature != null)
        {
            var name = mapInfo.Feature["Name"]?.ToString();
            var desc = mapInfo.Feature["Description"]?.ToString();

            if (!string.IsNullOrEmpty(name))
            {
                // Chạy trên luồng giao diện chính
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    bool wantToListen = await DisplayAlert(
                        name,
                        desc,
                        "Nghe thuyết minh",
                        "Đóng"
                    );

                    if (wantToListen)
                    {
                        await SpeakDescription(desc);
                    }
                });
            }
        }
    }

    private async Task SpeakDescription(string text)
    {
        // Hủy lần đọc trước đó nếu đang đọc dở
        if (_speechCts != null)
        {
            _speechCts.Cancel();
            _speechCts.Dispose();
        }

        // Tạo "cờ lệnh" mới cho lần đọc này
        _speechCts = new CancellationTokenSource();

        try
        {
            //Cấu hình giọng đọc Tiếng Việt
            var locales = await TextToSpeech.Default.GetLocalesAsync();
            string savedLang = Preferences.Get("VoiceLanguage", "vi");
            var voice = locales.FirstOrDefault(l => l.Language.StartsWith(savedLang));
            var options = new SpeechOptions
            {
                Locale = voice,
                Volume = 1.0f,
                Pitch = 1.0f
            };

            if (voice != null)
            {
                options.Locale = voice;
                System.Diagnostics.Debug.WriteLine($"---> Đang đọc bằng giọng: {voice.Name}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("---> CẢNH BÁO: Máy này chưa cài Tiếng Việt! Sẽ đọc bằng giọng mặc định.");
            }

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                    await TextToSpeech.Default.SpeakAsync(text, options, _speechCts.Token);
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"---> Lỗi cài đặt TTS: {ex.Message}");
        }
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

    // Hàm dừng lắng nghe
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