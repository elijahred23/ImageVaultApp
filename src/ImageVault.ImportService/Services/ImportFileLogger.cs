using ImageVault.ImportService.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ImageVault.ImportService.Services;

public sealed class ImportFileLogger
{
    private readonly string _logPath;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public ImportFileLogger(IHostEnvironment environment, IOptions<ImageImportOptions> options)
    {
        var dropRoot = ResolvePath(environment.ContentRootPath, options.Value.DropRoot);
        var logFolder = Path.Combine(dropRoot, options.Value.LogFolder);

        Directory.CreateDirectory(logFolder);

        _logPath = Path.Combine(logFolder, "image-importer.log");
    }

    public async Task WriteAsync(string message, CancellationToken cancellationToken)
    {
        var line = $"{DateTimeOffset.UtcNow:O} {message}{Environment.NewLine}";

        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            await File.AppendAllTextAsync(_logPath, line, cancellationToken);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private static string ResolvePath(string contentRootPath, string path)
    {
        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(contentRootPath, path));
    }
}
