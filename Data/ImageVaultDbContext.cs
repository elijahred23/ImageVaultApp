using Microsoft.EntityFrameworkCore;

namespace ImageVaultApp.Data
{

    public class ImageVaultDbContext : DbContext
    {
        public ImageVaultDbContext(DbContextOptions<ImageVaultDbContext> options)
            :base(options) {}
        public DbSet<UserSettings> UserSettings => Set<UserSettings>();
        public DbSet<Image> Images => Set<Image>();
        public DbSet<Favorite> Favorites => Set<Favorite>();

    }
}
