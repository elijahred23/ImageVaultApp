using Microsoft.EntityFrameworkCore;
public class ImageVaultDbContext : DbContext
{
    public ImageVaultDbContext(DbContextOptions<ImageVaultDbContext> options)
        :base(options) {}
    public DbSet<UserSettings> UserSettings => Set<UserSettings>();

}