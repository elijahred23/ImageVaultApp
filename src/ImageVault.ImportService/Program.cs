using ImageVault.ImportService.Data;
using ImageVault.ImportService.Options;
using ImageVault.ImportService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var projectRoot = FindAncestorContaining(AppContext.BaseDirectory, "ImageVault.ImportService.csproj")
    ?? Directory.GetCurrentDirectory();
var repositoryRoot = Path.GetFullPath(Path.Combine(projectRoot, "..", ".."));

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = projectRoot
});

builder.Configuration
    .AddJsonFile(Path.Combine(projectRoot, "appsettings.json"), optional: true, reloadOnChange: true)
    .AddJsonFile(
        Path.Combine(projectRoot, $"appsettings.{builder.Environment.EnvironmentName}.json"),
        optional: true,
        reloadOnChange: true)
    .AddJsonFile(Path.Combine(repositoryRoot, "appsettings.json"), optional: true, reloadOnChange: true)
    .AddJsonFile(
        Path.Combine(repositoryRoot, $"appsettings.{builder.Environment.EnvironmentName}.json"),
        optional: true,
        reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args);

builder.Services.Configure<ImageImportOptions>(
    builder.Configuration.GetSection(ImageImportOptions.SectionName));

builder.Services.AddDbContextFactory<ImageVaultImportDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("ImageVaultDb");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("Connection string 'ImageVaultDb' is required.");
    }

    options.UseSqlServer(connectionString);
});

builder.Services.AddSingleton<ImportFileLogger>();
builder.Services.AddHostedService<ImageImportWorker>();

var host = builder.Build();
host.Run();

static string? FindAncestorContaining(string startPath, string fileName)
{
    var directory = new DirectoryInfo(startPath);

    while (directory is not null)
    {
        if (File.Exists(Path.Combine(directory.FullName, fileName)))
        {
            return directory.FullName;
        }

        directory = directory.Parent;
    }

    return null;
}
