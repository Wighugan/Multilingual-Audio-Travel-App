using Mapsui.Extensions;
using Mapsui.Projections;
using Mapsui.Tiling;
using Mapsui.Layers;
using Mapsui.Styles;
using Mapsui.Nts;
using Mapsui.UI.Maui;
using Mapsui; // Thêm namespace gốc để dùng MPoint

// Đặt tên ngắn gọn để tránh nhầm lẫn
using MColor = Mapsui.Styles.Color;
using MBrush = Mapsui.Styles.Brush;

namespace multilingualAudioTravelApp;

public partial class MainPage : ContentPage
{
    // Layer chứa dấu chấm đỏ
    private MemoryLayer _currentLocationLayer;

    public MainPage()
    {
        InitializeComponent();
        InitializeMap();
    }

    private void InitializeMap()
    {
        // 1. Thêm bản đồ nền OpenStreetMap
        MyMap.Map.Layers.Add(OpenStreetMap.CreateTileLayer());

        // 2. Tạo layer vẽ điểm vị trí
        _currentLocationLayer = new MemoryLayer
        {
            Name = "LocationLayer",
            // Style chung cho cả layer (vẽ dấu chấm đỏ viền trắng)
            Style = new SymbolStyle
            {
                Fill = new MBrush(MColor.Red),
                SymbolScale = 0.5,
                Outline = new Pen { Color = MColor.White, Width = 2 }
            }
        };
        MyMap.Map.Layers.Add(_currentLocationLayer);

        // 3. Mặc định bay về Việt Nam
        // Dùng Dispatcher để đảm bảo giao diện đã vẽ xong mới di chuyển camera
        Dispatcher.Dispatch(() =>
        {
            try
            {
                var centerVietnam = SphericalMercator.FromLonLat(108.0, 14.0);
                var point = new MPoint(centerVietnam.x, centerVietnam.y);

                // --- SỬA LỖI NAVIGATETO Ở ĐÂY ---
                // Thay vì NavigateTo, ta dùng CenterOn (chỉnh tâm) và ZoomTo (chỉnh độ xa)
                MyMap.Map.Navigator.CenterOn(point);
                MyMap.Map.Navigator.ZoomTo(2000); // 2000: nhìn thấy toàn quốc
            }
            catch { /* Bỏ qua lỗi khởi tạo nếu có */ }
        });
    }

    private async void OnMyLocationClicked(object sender, EventArgs e)
    {
        try
        {
            // Kiểm tra quyền
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted) return;
            }

            // Lấy tọa độ GPS
            var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(5));
            var location = await Geolocation.GetLocationAsync(request);

            if (location != null)
            {
                // Chuyển đổi tọa độ
                var smPoint = SphericalMercator.FromLonLat(location.Longitude, location.Latitude);
                var mapPoint = new MPoint(smPoint.x, smPoint.y);

                // --- SỬA LỖI LIST FEATURE Ở ĐÂY ---
                // Tạo một điểm mới
                var pointFeature = new PointFeature(mapPoint);

                // Khai báo rõ ràng là List<IFeature> (Danh sách các Interface Feature)
                // Mapsui yêu cầu kiểu IFeature chứ không phải Feature thường
                var features = new List<IFeature>();
                features.Add(pointFeature);

                // Cập nhật vào layer
                _currentLocationLayer.Features = features;
                _currentLocationLayer.DataHasChanged(); // Báo vẽ lại

                // Bay đến vị trí (Zoom mức 2: thấy đường phố)
                MyMap.Map.Navigator.CenterOn(mapPoint);
                MyMap.Map.Navigator.ZoomTo(2);
            }
            else
            {
                await DisplayAlert("Lỗi", "Không tìm thấy tọa độ. Hãy set Location trong Emulator.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", ex.Message, "OK");
        }
    }
}