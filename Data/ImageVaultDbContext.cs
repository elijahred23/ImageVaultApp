using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;

namespace ImageVaultApp.Data
{

    public class ImageVaultDbContext : DbContext
    {
        public ImageVaultDbContext(DbContextOptions<ImageVaultDbContext> options)
            :base(options) {}
        public DbSet<UserSettings> UserSettings => Set<UserSettings>();
        public DbSet<Image> Images => Set<Image>();

    }
}