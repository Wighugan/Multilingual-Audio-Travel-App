using SQLite;
using System.Text.Json;
using static multilingualAudioTravelApp.Services.PoiEntity;
using System.Net.Http.Json;


namespace multilingualAudioTravelApp.Services;

public class PoiTranslation
{
    public string Name { get; set; }
    public string Description { get; set; }
}

public class PoiEntity  //POI
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Image { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Radius { get; set; }
    public int Priority { get; set; }
    public int CooldownMinutes { get; set; } = 5;
    public string TranslationsJson { get; set; }
    private Dictionary<string, PoiTranslation> _parsedTranslations;
    
    [Ignore]
    public string FullImageUrl
    {
        get
        {
            if (string.IsNullOrEmpty(Image))
                return "placeholder.png";

            string firstImage = Image.Split(',').FirstOrDefault();

            if (!firstImage.Contains("."))
                return firstImage;

            string baseUrl = DeviceInfo.Platform == DevicePlatform.Android
                             ? "http://10.0.2.2:5068/images/"
                             : "http://localhost:5068/images/";

            return $"{baseUrl}{firstImage}";
        }
    }
    [Ignore]
    public List<string> ImageUrls
    {
        get
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(Image))
            {
                list.Add("placeholder.png");
                return list;
            }

            string baseUrl = DeviceInfo.Platform == DevicePlatform.Android
                             ? "http://10.0.2.2:5068/images/"
                             : "http://localhost:5068/images/";

            var fileNames = Image.Split(',');
            foreach (var fileName in fileNames)
            {
                if (fileName.Contains("."))
                    list.Add($"{baseUrl}{fileName}");
                else
                    list.Add(fileName);
            }
            return list;
        }
    }
    public class FavoriteEntity  //favoritePOI
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string UserEmail { get; set; }  // mỗi user có favorites riêng
        public string PoiName { get; set; }
        public string PoiImage { get; set; }
        public string PoiDescription { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class FeedbackEntity  //feedback
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string UserEmail { get; set; }
        public int Rating { get; set; }       
        public string Content { get; set; }
        public string CreatedAt { get; set; }
    }

    // Hàm tự động dịch chuỗi JSON thành Dictionary (Từ điển)
    [Ignore]
    public Dictionary<string, PoiTranslation> TranslationDict
    {
        get
        {
            try {
                if (_parsedTranslations == null)
                {
                    if (string.IsNullOrWhiteSpace(TranslationsJson))
                        _parsedTranslations = new Dictionary<string, PoiTranslation>();
                    else
                        _parsedTranslations = JsonSerializer.Deserialize<Dictionary<string, PoiTranslation>>(TranslationsJson);
                }
                return _parsedTranslations;
            }
            catch
            {
                return new Dictionary<string, PoiTranslation>();
            }
        }
    }

    [Ignore]
    public string CurrentName
    {
        get
        {
            string lang = Preferences.Get("AppLanguage", "vi");
            if (TranslationDict.ContainsKey(lang)) return TranslationDict[lang].Name;
            return TranslationDict.ContainsKey("vi") ? TranslationDict["vi"].Name : "Đang cập nhật...";
        }
    }

    [Ignore]
    public string CurrentDescription
    {
        get
        {
            string lang = Preferences.Get("AppLanguage", "vi");
            if (TranslationDict.ContainsKey(lang)) return TranslationDict[lang].Description;
            return TranslationDict.ContainsKey("vi") ? TranslationDict["vi"].Description : "";
        }
    }
}

    public class UserEntity  //user
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public string FullName { get; set; }
}
    public class DatabaseService
{
    private SQLiteAsyncConnection _db;
    private readonly string _dbPath;
    private string ApiBaseUrl => DeviceInfo.Platform == DevicePlatform.Android
        ? "http://10.0.2.2:5068"
        : "http://localhost:5068";

    public DatabaseService()
    {
        _dbPath = Path.Combine(FileSystem.AppDataDirectory, "tourapp.db");
    }

    public async Task InitAsync()
    {
        if (_db != null) return;

        _db = new SQLiteAsyncConnection(_dbPath);
    //   await _db.DropTableAsync<PoiEntity>();       // ← thêm tạm, xóa sau khi chạy 1 lần

        await _db.CreateTableAsync<PoiEntity>();
        await _db.CreateTableAsync<UserEntity>();
        await _db.CreateTableAsync<FavoriteEntity>();
        await _db.CreateTableAsync<FeedbackEntity>();
        var count = await _db.Table<PoiEntity>().CountAsync();
        if (count == 0)
            await SeedDataAsync();
    }

    public async Task<List<PoiEntity>> GetAllPoisAsync()
    {
        try
        {
            using var client = new HttpClient();

            string baseUrl = DeviceInfo.Platform == DevicePlatform.Android
                             ? "http://10.0.2.2:5068"
                             : "http://localhost:5068";

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            // Gọi lên Web API để xin danh sách quán ăn
            var poisFromServer = await client.GetFromJsonAsync<List<PoiEntity>>($"{baseUrl}/api/pois");

            if (poisFromServer != null && poisFromServer.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine("Đã lấy dữ liệu thành công từ Web API!");
                return poisFromServer;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LỖI MẠNG HOẶC CHƯA MỞ SERVER: {ex.Message}");
        }
        await InitAsync();
        return await _db.Table<PoiEntity>().ToListAsync();
    }

    public async Task AddPoiAsync(PoiEntity poi)
    {
        await InitAsync();
        await _db.InsertAsync(poi);
    }

    public async Task UpdatePoiAsync(PoiEntity poi)
    {
        await InitAsync();
        await _db.UpdateAsync(poi);
    }

    public async Task DeletePoiAsync(int id)
    {
        await InitAsync();
        await _db.DeleteAsync<PoiEntity>(id);
    }

    /*private async Task SeedDataAsync()
    {
        var samples = new List<PoiEntity>
        {
            new PoiEntity { 
                Name = "Khu phố ẩm thực Vĩnh Khánh", 
                Description = "Phố ẩm thực nổi tiếng quận 4 với rất nhiều món ngon hấp dẫn.",
                Image = "vinhkhanh.jpg",
                Latitude = 10.761923,
                Longitude = 106.701964,
                Radius = 100,
                Priority = 10 },
            new PoiEntity { 
                Name = "Quán Ốc Oanh", 
                Description = "Quán ốc lâu đời và nổi tiếng nhất khu Vĩnh Khánh.", 
                Image = "ocoanh.jpg", 
                Latitude = 10.761410, 
                Longitude = 106.702820, 
                Radius = 80, 
                Priority = 8 },
            new PoiEntity { 
                Name = "Quán Ốc Phát", 
                Description = "Ốc Phát Vĩnh Khánh vẫn luôn là điểm đến quen thuộc cho các tín đồ mê ốc.", 
                Image = "ocphat.jpg", 
                Latitude = 10.761921, 
                Longitude = 106.702151, 
                Radius = 70, 
                Priority = 6 },
            new PoiEntity { 
                Name = "Quán bún cá Châu Đốc", 
                Description = "Nước lèo thanh mát, đậm đà hương vị ăn cùng bún, chả cá và cá.", 
                Image = "bunca.jpg", 
                Latitude = 10.761455, 
                Longitude = 106.702660, 
                Radius = 70, 
                Priority = 6 },
            new PoiEntity { 
                Name = "Quán Ốc Loan", 
                Description = "Không gian thoáng mát, nổi tiếng với nước chấm cực kỳ ngon.", 
                Image = "ocloan.jpg", 
                Latitude = 10.761170, 
                Longitude = 106.702710, 
                Radius = 70, 
                Priority = 6 }
        };
        await _db.InsertAllAsync(samples);
    }*/
    private async Task SeedDataAsync()
    {
        var samples = new List<PoiEntity>
    {
        new PoiEntity
        {
            Image = "vinhkhanh.jpg", Latitude = 10.761923, Longitude = 106.701964 , Radius = 100, Priority = 10,
            TranslationsJson = JsonSerializer.Serialize(new Dictionary<string, PoiTranslation>
            {
                { "vi", new PoiTranslation { Name = "Khu phố Vĩnh Khánh", Description = "Phố ẩm thực nổi tiếng quận 4." } },
                { "en", new PoiTranslation { Name = "Vinh Khanh Food Street", Description = "A famous food street in District 4." } },
                { "ja", new PoiTranslation { Name = "ヴィンカン通り", Description = "4区で有名なグルメ通りです。" } },
                { "zh", new PoiTranslation { Name = "永庆美食街", Description = "第四郡著名的美食街。" } },
                { "ko", new PoiTranslation { Name = "빈칸 음식 거리", Description = "4군에서 유명한 음식 거리입니다." } }
            })
        },

        new PoiEntity
        {
            Image = "ocoanh.jpg", Latitude = 10.761410, Longitude = 106.702820 , Radius = 80, Priority = 8,
            TranslationsJson = JsonSerializer.Serialize(new Dictionary<string, PoiTranslation>
            {
                { "vi", new PoiTranslation { Name = "Quán ốc Oanh", Description = "Quán ốc lâu đời và nổi tiếng nhất khu Vĩnh Khánh." } },
                { "en", new PoiTranslation { Name = "Oanh Snail Restaurant", Description = "The oldest and most famous snail restaurant in Vinh Khanh area." } },
                { "ja", new PoiTranslation { Name = "オアン貝料理店", Description = "ヴィンカンエリアで最も古く、最も有名な貝料理店です。" } },
                { "zh", new PoiTranslation { Name = "Oanh 螺店", Description = "永庆地区历史最悠久、最著名的海螺海鲜店。" } },
                { "ko", new PoiTranslation { Name = "오아잉 달팽이 식당", Description = "빈칸 지역에서 가장 오래되고 유명한 달팽이 요리 전문점입니다." } }
            })
        },

        new PoiEntity
        {
            Image = "ocphat.jpg", Latitude = 10.761921, Longitude = 106.702151, Radius = 70, Priority = 6,
            TranslationsJson = JsonSerializer.Serialize(new Dictionary<string, PoiTranslation>
            {
                { "vi", new PoiTranslation { Name = "Quán Ốc Phát", Description = "Ốc Phát Vĩnh Khánh vẫn luôn là điểm đến quen thuộc cho các tín đồ mê ốc." } },
                { "en", new PoiTranslation { Name = "Phat Snail Restaurant", Description = "Phat Snail Vinh Khanh is always a familiar destination for snail lovers." } },
                { "ja", new PoiTranslation { Name = "ファット貝料理店", Description = "ヴィンカン通りのファット貝料理店は、貝好きに常におなじみの場所です。" } },
                { "zh", new PoiTranslation { Name = "Phat 螺店", Description = "永庆的 Phat 螺店一直是海鲜爱好者的熟悉去处。" } },
                { "ko", new PoiTranslation { Name = "팟 달팽이 식당", Description = "빈칸의 팟 달팽이 식당은 달팽이 요리를 사랑하는 사람들에게 항상 친숙한 장소입니다." } }
            })
        },

        new PoiEntity
        {
            Image = "bunca.jpg", Latitude = 10.761455, Longitude = 106.702660, Radius = 70, Priority = 6,
            TranslationsJson = JsonSerializer.Serialize(new Dictionary<string, PoiTranslation>
            {
                { "vi", new PoiTranslation { Name = "Quán bún cá Châu Đốc", Description = "Nước lèo thanh mát, đậm đà hương vị ăn cùng bún, chả cá và cá." } },
                { "en", new PoiTranslation { Name = "Chau Doc Fish Noodle Soup", Description = "Refreshing, flavorful broth served with noodles, fish cake, and fish." } },
                { "ja", new PoiTranslation { Name = "チャウドック魚麺", Description = "さっぱりとして風味豊かなスープに、麺、さつま揚げ、魚が入っています。" } },
                { "zh", new PoiTranslation { Name = "朱笃鱼汤粉", Description = "清凉浓郁的汤汁，配上米粉、鱼饼和鱼肉，风味绝佳。" } },
                { "ko", new PoiTranslation { Name = "쩌우독 생선 국수", Description = "시원하고 진한 풍미의 국물에 국수, 어묵, 생선이 함께 제공됩니다." } }
            })
        },

        new PoiEntity
        {
            Image = "ocloan.jpg", Latitude = 10.761170, Longitude = 106.702710, Radius = 70, Priority = 6,
            TranslationsJson = JsonSerializer.Serialize(new Dictionary<string, PoiTranslation>
            {
                { "vi", new PoiTranslation { Name = "Quán Ốc Loan", Description = "Không gian thoáng mát, nổi tiếng với nước chấm cực kỳ ngon." } },
                { "en", new PoiTranslation { Name = "Loan Snail Restaurant", Description = "Airy space, famous for its extremely delicious dipping sauce." } },
                { "ja", new PoiTranslation { Name = "ロアン貝料理店", Description = "風通しの良い空間で、とても美味しいディップソースで有名です。" } },
                { "zh", new PoiTranslation { Name = "Loan 螺店", Description = "空间通风舒适，以极其美味的特制蘸酱而闻名。" } },
                { "ko", new PoiTranslation { Name = "로안 달팽이 식당", Description = "통풍이 잘 되는 쾌적한 공간과 매우 맛있는 디핑 소스로 유명합니다." } }
            })
        },

        new PoiEntity
        {
            Image = "sushiko.jpg", Latitude = 10.760827, Longitude = 106.704798, Radius = 70, Priority = 6,
            TranslationsJson = JsonSerializer.Serialize(new Dictionary<string, PoiTranslation>
            {
                { "vi", new PoiTranslation { Name = "Nhà hàng Sushi KO", Description = "Nhiều món độc đáo khác, tạo nên trải nghiệm ẩm thực phong phú và hấp dẫn." } },
                { "en", new PoiTranslation { Name = "Sushi KO Restaurant", Description = "A variety of unique dishes creating a rich and exciting culinary experience." } },
                { "ja", new PoiTranslation { Name = "寿司KOレストラン", Description = "豊かで魅力的な食体験を生み出す、多彩なユニークな料理が揃っています。" } },
                { "zh", new PoiTranslation { Name = "Sushi KO 寿司店", Description = "提供多种特色菜肴，打造丰富诱人的美食体验。" } },
                { "ko", new PoiTranslation { Name = "스시 KO 레스토랑", Description = "다양하고 독특한 요리로 풍부하고 매력적인 미식 경험을 선사합니다." } }
            })
        },

        new PoiEntity
        {
            Image = "chili.jpg", Latitude = 10.760774, Longitude = 106.704575, Radius = 70, Priority = 6,
            TranslationsJson = JsonSerializer.Serialize(new Dictionary<string, PoiTranslation>
            {
                { "vi", new PoiTranslation { Name = "Nhà hàng nướng Chilli", Description = "Giá cả hợp lý và thực đơn đồ nướng tự chọn đa dạng. Các món ăn ở đây được tẩm ướp đậm đà, nêm nếm khéo léo." } },
                { "en", new PoiTranslation { Name = "Chilli BBQ Restaurant", Description = "Affordable prices with a diverse all-you-can-grill menu. Dishes are richly marinated and skillfully seasoned." } },
                { "ja", new PoiTranslation { Name = "チリBBQレストラン", Description = "手頃な価格で多彩な食べ放題グリルメニューを提供。料理はしっかりと下味が付けられ、丁寧に味付けされています。" } },
                { "zh", new PoiTranslation { Name = "Chilli 烧烤餐厅", Description = "价格合理，自助烧烤菜单丰富。这里的菜肴腌制入味，调味巧妙。" } },
                { "ko", new PoiTranslation { Name = "칠리 BBQ 레스토랑", Description = "합리적인 가격과 다양한 무한 리필 바비큐 메뉴. 요리들은 양념이 잘 배어 있고 훌륭하게 간이 되어 있습니다." } }
            })
        },

        new PoiEntity
        {
            Image = "otxiemquan.jpg", Latitude = 10.760801, Longitude = 106.704631, Radius = 70, Priority = 6,
            TranslationsJson = JsonSerializer.Serialize(new Dictionary<string, PoiTranslation>
            {
                { "vi", new PoiTranslation { Name = "Ớt Xiêm Quán", Description = "Món ăn ngon, phù hợp với khẩu vị nhiều người và có giá cả vô cùng hợp lý. Menu đa dạng" } },
                { "en", new PoiTranslation { Name = "Ot Xiem Restaurant", Description = "Delicious food that suits many tastes at very affordable prices. Features a diverse and varied menu." } },
                { "ja", new PoiTranslation { Name = "オットシエムレストラン", Description = "多くの人の好みに合う美味しい料理を手頃な価格で提供。バラエティ豊かな多彩なメニューが揃っています。" } },
                { "zh", new PoiTranslation { Name = "Ot Xiem 餐厅", Description = "菜肴美味，符合大众口味，价格非常合理。菜单丰富多样。" } },
                { "ko", new PoiTranslation { Name = "엇시엠 식당", Description = "많은 사람의 입맛에 맞는 맛있는 요리를 매우 합리적인 가격에 제공합니다. 다양하고 풍부한 메뉴를 자랑합니다." } }
            })
        }
    };

        await _db.InsertAllAsync(samples);
    }

    // Đăng ký tài khoản mới
    public async Task<bool> RegisterAsync(string email, string password, string fullName)
    {
        try
        {
            using var client = new HttpClient();
            string baseUrl = DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:5068" : "http://localhost:5068";

            // kéo danh sách về để check trùng email
            var users = await client.GetFromJsonAsync<List<UserEntity>>($"{baseUrl}/api/users");
            if (users != null && users.Any(u => u.Email == email))
                return false; // Email đã tồn tại

            // đóng gói và gửi lệnh post tạo mới
            var newUser = new UserEntity { Email = email, Password = password, FullName = fullName };
            var response = await client.PostAsJsonAsync($"{baseUrl}/api/users", newUser);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Lỗi Đăng ký: {ex.Message}");
            return false;
        }
    }

    // Đăng nhập - kiểm tra email + password
    public async Task<UserEntity> LoginAsync(string email, string password)
    {
        try
        {
            using var client = new HttpClient();
            string baseUrl = DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:5068" : "http://localhost:5068";

            var users = await client.GetFromJsonAsync<List<UserEntity>>($"{baseUrl}/api/users");

            return users?.FirstOrDefault(u => u.Email == email && u.Password == password);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Lỗi Đăng nhập: {ex.Message}");
            return null;
        }
    }

    // Cập nhật tên + email
    public async Task<bool> UpdateProfileAsync(string currentEmail, string newName, string newEmail, string newPassword = null)
    {
        try
        {
            using var client = new HttpClient();
            string baseUrl = DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:5068" : "http://localhost:5068";

            var users = await client.GetFromJsonAsync<List<UserEntity>>($"{baseUrl}/api/users");
            var user = users?.FirstOrDefault(u => u.Email == currentEmail);

            if (user == null) return false;

            // Đổi thông tin
            user.FullName = newName;
            user.Email = newEmail;
            if (!string.IsNullOrEmpty(newPassword)) user.Password = newPassword;

            var response = await client.PutAsJsonAsync($"{baseUrl}/api/users/{user.Id}", user);

            if (response.IsSuccessStatusCode)
            {
                Preferences.Set("userName", newName);
                Preferences.Set("userEmail", newEmail);
                return true;
            }
            return false;
        }
        catch { return false; }
    }


    // Lấy danh sách yêu thích của user
    public async Task<List<FavoriteEntity>> GetFavoritesAsync(string userEmail)
    {
        try
        {
            using var client = new HttpClient();
            var encodedEmail = Uri.EscapeDataString(userEmail ?? string.Empty);
            var favorites = await client.GetFromJsonAsync<List<FavoriteEntity>>(
                $"{ApiBaseUrl}/api/favorites?userEmail={encodedEmail}");

            if (favorites != null)
                return favorites;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Lỗi lấy favorites từ server: {ex.Message}");
        }

        await InitAsync();
        return await _db.Table<FavoriteEntity>()
            .Where(f => f.UserEmail == userEmail)
            .ToListAsync();
    }

    // Kiểm tra đã bookmark chưa
    public async Task<bool> IsFavoriteAsync(string userEmail, string poiName)
    {
        var list = await GetFavoritesAsync(userEmail);
        return list.Any(f => f.PoiName == poiName);
    }

    // Thêm vào yêu thích
    // Thêm vào yêu thích — sửa lại để lưu đúng URL ảnh
    public async Task AddFavoriteAsync(string userEmail, PoiEntity poi)
    {
        // Kiểm tra trùng trước
        var exists = await IsFavoriteAsync(userEmail, poi.CurrentName);
        if (exists) return;

        var favorite = new FavoriteEntity
        {
            UserEmail = userEmail,
            PoiName = poi.CurrentName,       // tên theo ngôn ngữ hiện tại
            PoiImage = poi.FullImageUrl,       // URL đầy đủ → hiển thị được
            PoiDescription = poi.CurrentDescription,
            Latitude = poi.Latitude,
            Longitude = poi.Longitude
        };

        // Ưu tiên ghi lên SQL Server qua API
        try
        {
            using var client = new HttpClient();
            var response = await client.PostAsJsonAsync(
                $"{ApiBaseUrl}/api/favorites", favorite);

            if (response.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[Favorite] Đã lưu '{poi.CurrentName}' lên SQL Server.");
                return;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[Favorite] Lỗi lưu lên server, fallback SQLite: {ex.Message}");
        }

        // Fallback: ghi vào SQLite local nếu mất mạng
        await InitAsync();
        await _db.InsertAsync(favorite);
    }

    // Xóa khỏi yêu thích
    public async Task RemoveFavoriteAsync(string userEmail, string poiName)
    {
        try
        {
            using var client = new HttpClient();
            var encodedEmail = Uri.EscapeDataString(userEmail ?? string.Empty);
            var encodedPoiName = Uri.EscapeDataString(poiName ?? string.Empty);
            var response = await client.DeleteAsync(
                $"{ApiBaseUrl}/api/favorites/by-user?userEmail={encodedEmail}&poiName={encodedPoiName}");
            if (response.IsSuccessStatusCode)
                return;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Lỗi xóa favorite trên server: {ex.Message}");
        }

        await InitAsync();
        var item = await _db.Table<FavoriteEntity>()
            .Where(f => f.UserEmail == userEmail && f.PoiName == poiName)
            .FirstOrDefaultAsync();
        if (item != null)
            await _db.DeleteAsync(item);
    }

    //luu feedback
    public async Task SaveFeedbackAsync(string email, int rating, string content)
    {
        try
        {
            using var client = new HttpClient();
            string baseUrl = DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:5068" : "http://localhost:5068";

            var newFeedback = new FeedbackEntity
            {
                UserEmail = email,
                Rating = rating,
                Content = content,
                CreatedAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm")
            };

            var response = await client.PostAsJsonAsync($"{baseUrl}/api/feedbacks", newFeedback);

            if (response.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine("Đã gửi Feedback lên Server thành công!");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Lỗi gửi Feedback: {ex.Message}");
        }
    }
}