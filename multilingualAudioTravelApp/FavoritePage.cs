using multilingualAudioTravelApp.Services;
using static multilingualAudioTravelApp.Services.PoiEntity;

namespace multilingualAudioTravelApp;

public partial class FavoritePage : ContentPage
{
    private readonly DatabaseService _dbService = new DatabaseService();
    private bool _removeTapping = false;

    public FavoritePage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadFavorites();
    }

    private async Task LoadFavorites()
    {
        var email = Preferences.Get("userEmail", "");
        var favorites = await _dbService.GetFavoritesAsync(email);
        FavCollection.ItemsSource = favorites;
    }

    private async void OnFavoriteRowTapped(object sender, TappedEventArgs e)
    {
        if (_removeTapping) return;
        if (e.Parameter is not FavoriteEntity fav) return;

        var entities = await _dbService.GetAllPoisAsync();
        var poi = entities.FirstOrDefault(p => p.CurrentName == fav.PoiName);

        PoiCardItem card;
        if (poi != null)
        {
            card = new PoiCardItem
            {
                Name = poi.CurrentName,
                Image = poi.Image,
                FullImageUrl = poi.FullImageUrl,
                ImageUrls = poi.ImageUrls,
                Description = poi.CurrentDescription,
                Latitude = poi.Latitude,
                Longitude = poi.Longitude,
                SourcePoi = poi,
                IsFavorite = true
            };
        }
        else
        {
            var img = string.IsNullOrWhiteSpace(fav.PoiImage) ? "placeholder.png" : fav.PoiImage.Trim();
            card = new PoiCardItem
            {
                Name = fav.PoiName,
                Description = fav.PoiDescription ?? "",
                Latitude = fav.Latitude,
                Longitude = fav.Longitude,
                FullImageUrl = img,
                ImageUrls = new List<string> { img },
                SourcePoi = null,
                IsFavorite = true
            };
        }

        HomePage.PendingPoiToShow = card;
        await Shell.Current.GoToAsync("//HomePage");
    }

    private async void OnRemoveFavoriteTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not FavoriteEntity item) return;

        _removeTapping = true;
        try
        {
            bool confirm = await DisplayAlert(
                "Bỏ yêu thích",
                $"Bỏ \"{item.PoiName}\" khỏi danh sách?",
                "Bỏ lưu", "Hủy");

            if (!confirm) return;

            var email = Preferences.Get("userEmail", "");
            await _dbService.RemoveFavoriteAsync(email, item.PoiName);
            await LoadFavorites();
        }
        finally
        {
            _removeTapping = false;
        }
    }
}