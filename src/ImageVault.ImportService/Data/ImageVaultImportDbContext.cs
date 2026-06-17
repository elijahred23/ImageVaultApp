using ImageVault.ImportService.Models;
using Microsoft.EntityFrameworkCore;

namespace ImageVault.ImportService.Data;

public class ImageVaultImportDbContext : DbContext
{
    public ImageVaultImportDbContext(DbContextOptions<ImageVaultImportDbContext> options)
        : base(options)
    {
    }

    public DbSet<Image> Images => Set<Image>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Image>().ToTable("Images");

        base.OnModelCreating(modelBuilder);
    }
}
