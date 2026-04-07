using System.ComponentModel.DataAnnotations;

namespace TravelApp.WebAdmin.Models
{
    public class PoiEntity
    {
        [Key] 
        public int Id { get; set; }
        public string Image { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Radius { get; set; }
        public int Priority { get; set; }
        public int CooldownMinutes { get; set; } = 5;
        public string TranslationsJson { get; set; }
    }

    public class UserEntity
    {
        [Key]
        public int Id { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }
    }

    public class FavoriteEntity
    {
        [Key]
        public int Id { get; set; }
        public string UserEmail { get; set; }
        public string PoiName { get; set; }
        public string PoiImage { get; set; }
        public string PoiDescription { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class FeedbackEntity
    {
        [Key]
        public int Id { get; set; }
        public string UserEmail { get; set; }
        public int Rating { get; set; }
        public string Content { get; set; }
        public string CreatedAt { get; set; }
    }
}