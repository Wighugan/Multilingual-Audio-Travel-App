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
    private bool _isTracking = false; // Biến cờ để kiểm soát việc theo dõi
    private CancellationTokenSource _speechCts;
    private List<PoiData> _poiList = new List<PoiData>();
    public MainPage()
    {
        InitializeComponent();
        InitializeMap();
    }

    private void InitializeMap()
    {
        var minVN = SphericalMercator.FromLonLat(102.0, 8.0);
        var maxVN = SphericalMercator.FromLonLat(110.0, 24.0);
        var panBounds = new MRect(minVN.x, minVN.y, maxVN.x, maxVN.y);

        MyMap.Map.Layers.Add(OpenStreetMap.CreateTileLayer());

        MyMap.Map.Navigator.OverridePanBounds = panBounds;
        MyMap.Map.Navigator.OverrideZoomBounds = new MMinMax(0.5, 5000);

        // Layer dấu chấm đỏ
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

        // Mặc định nhìn về VN
        Dispatcher.Dispatch(() =>
        {
            try
            {
                var centerVietnam = SphericalMercator.FromLonLat(108.0, 14.0);
                MyMap.Map.Navigator.CenterOn(new MPoint(centerVietnam.x, centerVietnam.y));
                MyMap.Map.Navigator.ZoomTo(1);
            }
            catch { }
        });
    }
    public class PoiData
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool HasPlayed { get; set; } = false;

        public double Radius { get; set; } = 50;
        public int Priority { get; set; } = 1;
    }

    // Hàm tạo các điểm du lịch mẫu
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

        var dinhDocLap = new PoiData
        {
            Name = "Dinh Độc Lập",
            Description = "Dinh Độc Lập là di tích lịch sử nổi tiếng tại Thành phố Hồ Chí Minh, nơi đánh dấu sự kiện thống nhất đất nước.",
            Latitude = 10.77782,
            Longitude = 106.69529,
            Radius = 100, //100m
            Priority = 10 
        };
        _poiList.Add(dinhDocLap);

        var hoConRua = new PoiData
        {
            Name = "Hồ Con Rùa",
            Latitude = 10.7825,
            Longitude = 106.6961,
            Description = "Đây là Hồ Con Rùa, một điểm đến văn hóa, du lịch đặc trưng với kiến trúc tháp cao, hồ phun nước và không gian xanh thoáng mát. Đây cũng là thiên đường ẩm thực đường phố sôi động về đêm, thu hút đông đảo người dân và du khách.",

            Radius = 30,   // 30m
            Priority = 5
        };
        _poiList.Add(hoConRua);

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
            var voice = locales.FirstOrDefault(l => l.Language == "vi");
            var options = new SpeechOptions
            {
                Locale = voice,
                Volume = 1.0f,
                Pitch = 1.0f
            };

            if (voice != null)
            {
                options.Locale = voice;
                System.Diagnostics.Debug.WriteLine($"---> Đã tìm thấy giọng: {voice.Name}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("---> CẢNH BÁO: Máy này chưa cài Tiếng Việt! Sẽ đọc bằng giọng mặc định.");
            }

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    await TextToSpeech.Default.SpeakAsync(text, options, _speechCts.Token);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"---> Lỗi khi đọc: {ex.Message}");
                }
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
            .Where(x => x.Distance <= x.Poi.Radius && !x.Poi.HasPlayed) 
            .OrderByDescending(x => x.Poi.Priority)
            .ThenBy(x => x.Distance)               
            .ToList();

        var bestMatch = candidates.FirstOrDefault();

        if (bestMatch != null)
        {
            var poi = bestMatch.Poi;

            poi.HasPlayed = true;

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