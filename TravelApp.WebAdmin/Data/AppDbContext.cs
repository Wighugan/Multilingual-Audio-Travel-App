using Microsoft.EntityFrameworkCore;
using TravelApp.WebAdmin.Models;

namespace TravelApp.WebAdmin.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<PoiEntity> Pois { get; set; }
        public DbSet<UserEntity> Users { get; set; }
        public DbSet<FavoriteEntity> Favorites { get; set; }
        public DbSet<FeedbackEntity> Feedbacks { get; set; }
        public DbSet<LanguageEntity> Languages { get; set; }
    }
}