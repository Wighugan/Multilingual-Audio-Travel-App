using Mapsui.Extensions;
using Mapsui.Projections;
using Mapsui.Tiling;
using Mapsui.Layers;
using Mapsui.Styles;
using Mapsui.Nts;
using Mapsui.UI.Maui;
using Mapsui; 
using Microsoft.Maui.Devices.Sensors;

using MColor = Mapsui.Styles.Color;
using MBrush = Mapsui.Styles.Brush;

namespace multilingualAudioTravelApp;

public partial class MainPage : ContentPage
{
    private MemoryLayer _currentLocationLayer;
    private bool _isTracking = false; // Biến cờ để kiểm soát việc theo dõi

    public MainPage()
    {
        InitializeComponent();
        InitializeMap();
    }

    private void InitializeMap()
    {
        // Bản đồ nền
        MyMap.Map.Layers.Add(OpenStreetMap.CreateTileLayer());

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

        // Mặc định nhìn về VN
        Dispatcher.Dispatch(() =>
        {
            try
            {
                var centerVietnam = SphericalMercator.FromLonLat(108.0, 14.0);
                MyMap.Map.Navigator.CenterOn(new MPoint(centerVietnam.x, centerVietnam.y));
                MyMap.Map.Navigator.ZoomTo(2000);
            }
            catch { }
        });
    }

    // 1. Khi màn hình hiện lên -> Bắt đầu theo dõi ngay
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await StartListeningGps();
    }

    // 2. Khi thoát màn hình/ẩn app -> Dừng theo dõi (để tiết kiệm pin)
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

        // Cấu hình: Cập nhật mỗi 2 giây hoặc khi đi được 5 mét (để đỡ tốn pin)
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
        // Sự kiện GPS chạy ở luồng phụ (Background Thread), 
        // muốn vẽ lên màn hình phải đưa về luồng chính (Main Thread)
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateUserLocationOnMap(e.Location);
        });
    }

    private void UpdateUserLocationOnMap(Location location)
    {
        if (location == null) return;

        try
        {
            // Tính toán tọa độ
            var smPoint = SphericalMercator.FromLonLat(location.Longitude, location.Latitude);
            var mapPoint = new MPoint(smPoint.x, smPoint.y);

            // Vẽ lại dấu chấm đỏ
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

/*    private void OnMyLocationClicked(object sender, EventArgs e)
    {
        // Khi bấm nút thì ép camera quay về ngay lập tức
        var lastLocation = _currentLocationLayer.Features.FirstOrDefault() as PointFeature;
        if (lastLocation != null)
        {
            MyMap.Map.Navigator.CenterOn(lastLocation.Point);
            MyMap.Map.Navigator.ZoomTo(2);
        }
    }*/
}