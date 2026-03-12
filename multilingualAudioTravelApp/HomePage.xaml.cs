using multilingualAudioTravelApp.Services;

namespace multilingualAudioTravelApp;

public class PoiCardItem
{
    public string Name { get; set; }
    public string Image { get; set; }
    public string Description { get; set; }
    public string ShortDescription => Description?.Length > 50
        ? Description.Substring(0, 50) + "..."
        : Description;
}

public partial class HomePage : ContentPage
{
    private readonly DatabaseService _dbService = new DatabaseService();
    private List<PoiCardItem> _allPois = new();
    private bool _isSearching = false; // đang trong chế độ tìm kiếm hay không

    public HomePage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadPois();
    }

    private async Task LoadPois()
    {
        var entities = await _dbService.GetAllPoisAsync();

        _allPois = entities.Select(e => new PoiCardItem
        {
            Name = e.CurrentName,
            Image = e.Image,
            Description = e.CurrentDescription
        }).ToList();

        PoiCollection.ItemsSource = _allPois;
    }

    // Theo dõi khi text thay đổi
    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var newText = e.NewTextValue;

        // Nếu đang tìm kiếm và xóa hết chữ → reset về tất cả
        if (_isSearching && string.IsNullOrEmpty(newText))
        {
            _isSearching = false;
            PoiCollection.ItemsSource = _allPois;
        }
    }

    // Chỉ tìm khi bấm Enter hoặc nút tìm kiếm
    private void OnSearchButtonPressed(object sender, EventArgs e)
    {
        var keyword = SearchBar.Text?.Trim().ToLower();

        if (string.IsNullOrEmpty(keyword))
        {
            _isSearching = false;
            PoiCollection.ItemsSource = _allPois;
            return;
        }

        _isSearching = true;

        var filtered = _allPois
            .Where(p => p.Name.ToLower().Contains(keyword)
                     || p.Description.ToLower().Contains(keyword))
            .ToList();

        PoiCollection.ItemsSource = filtered;
    }

    private async void OnPoiSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not PoiCardItem selected)
            return;

        ((CollectionView)sender).SelectedItem = null;
        await Shell.Current.GoToAsync("//MainPage");
    }
}