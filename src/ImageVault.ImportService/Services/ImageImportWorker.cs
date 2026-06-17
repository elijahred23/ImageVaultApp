using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Channels;
using ImageVault.ImportService.Data;
using ImageVault.ImportService.Models;
using ImageVault.ImportService.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ImageVault.ImportService.Services;

public sealed class ImageImportWorker : BackgroundService
{
    private readonly IDbContextFactory<ImageVaultImportDbContext> _dbContextFactory;
    private readonly ImportFileLogger _fileLogger;
    private readonly ILogger<ImageImportWorker> _logger;
    private readonly ImageImportOptions _options;
    private readonly string _processPath;
    private readonly string _processNsfwPath;
    private readonly string _processingPath;
    private readonly string _processedPath;
    private readonly string _errorPath;
    private readonly ConcurrentDictionary<string, byte> _queuedFiles = new(StringComparer.OrdinalIgnoreCase);
    private readonly Channel<FileImportRequest> _queue = Channel.CreateUnbounded<FileImportRequest>();
    private readonly List<FileSystemWatcher> _watchers = [];

    public ImageImportWorker(
        IDbContextFactory<ImageVaultImportDbContext> dbContextFactory,
        ImportFileLogger fileLogger,
        IHostEnvironment environment,
        IOptions<ImageImportOptions> options,
        ILogger<ImageImportWorker> logger)
    {
        _dbContextFactory = dbContextFactory;
        _fileLogger = fileLogger;
        _logger = logger;
        _options = options.Value;

        var dropRoot = ResolvePath(environment.ContentRootPath, _options.DropRoot);
        _processPath = Path.Combine(dropRoot, _options.ProcessFolder);
        _processNsfwPath = Path.Combine(dropRoot, _options.ProcessNsfwFolder);
        _processingPath = Path.Combine(dropRoot, _options.ProcessingFolder);
        _processedPath = Path.Combine(dropRoot, _options.ProcessedFolder);
        _errorPath = Path.Combine(dropRoot, _options.ErrorFolder);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        EnsureDirectories();
        StartWatcher(_processPath, isNsfw: false);
        StartWatcher(_processNsfwPath, isNsfw: true);

        await _fileLogger.WriteAsync("Image importer started.", stoppingToken);

        QueueExistingFiles(_processPath, isNsfw: false);
        QueueExistingFiles(_processNsfwPath, isNsfw: true);

        await foreach (var request in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessFileAsync(request, stoppingToken);
            }
            finally
            {
                _queuedFiles.TryRemove(request.Path, out _);
            }
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var watcher in _watchers)
        {
            watcher.Dispose();
        }

        return base.StopAsync(cancellationToken);
    }

    private void EnsureDirectories()
    {
        Directory.CreateDirectory(_processPath);
        Directory.CreateDirectory(_processNsfwPath);
        Directory.CreateDirectory(_processingPath);
        Directory.CreateDirectory(_processedPath);
        Directory.CreateDirectory(_errorPath);
    }

    private void StartWatcher(string path, bool isNsfw)
    {
        var watcher = new FileSystemWatcher(path, "*.json")
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
            EnableRaisingEvents = true
        };

        watcher.Created += (_, e) => QueueFile(e.FullPath, isNsfw);
        watcher.Renamed += (_, e) => QueueFile(e.FullPath, isNsfw);

        _watchers.Add(watcher);
        _logger.LogInformation("Watching {Folder} for JSON image imports. NSFW: {IsNsfw}", path, isNsfw);
    }

    private void QueueExistingFiles(string path, bool isNsfw)
    {
        foreach (var filePath in Directory.EnumerateFiles(path, "*.json"))
        {
            QueueFile(filePath, isNsfw);
        }
    }

    private void QueueFile(string path, bool isNsfw)
    {
        if (!_queuedFiles.TryAdd(path, 0))
        {
            return;
        }

        if (!_queue.Writer.TryWrite(new FileImportRequest(path, isNsfw)))
        {
            _queuedFiles.TryRemove(path, out _);
        }
    }

    private async Task ProcessFileAsync(FileImportRequest request, CancellationToken cancellationToken)
    {
        if (!File.Exists(request.Path))
        {
            return;
        }

        var sourceFileName = Path.GetFileName(request.Path);
        var processingPath = GetUniquePath(_processingPath, sourceFileName);
        var finalPath = string.Empty;

        try
        {
            await WaitUntilReadyAsync(request.Path, cancellationToken);
            File.Move(request.Path, processingPath);

            var imageSources = await ReadImageSourcesAsync(processingPath, cancellationToken);
            var title = Path.GetFileNameWithoutExtension(sourceFileName);

            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var images = imageSources.Select(source => new Image
            {
                UserId = _options.UserId,
                ImageUrl = source,
                Title = title,
                Description = title,
                IsNSFW = request.IsNsfw,
                CreatedAt = DateTime.UtcNow
            });

            dbContext.Images.AddRange(images);
            var importedCount = await dbContext.SaveChangesAsync(cancellationToken);

            finalPath = GetUniquePath(_processedPath, sourceFileName);
            File.Move(processingPath, finalPath);

            var message = $"Imported {importedCount} images from '{sourceFileName}'. NSFW: {request.IsNsfw}.";
            _logger.LogInformation("{Message}", message);
            await _fileLogger.WriteAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            finalPath = GetUniquePath(_errorPath, sourceFileName);

            if (File.Exists(processingPath))
            {
                File.Move(processingPath, finalPath);
            }
            else if (File.Exists(request.Path))
            {
                File.Move(request.Path, finalPath);
            }

            var message = $"Failed to import '{sourceFileName}'. Error file: '{finalPath}'. {ex.Message}";
            _logger.LogError(ex, "{Message}", message);
            await _fileLogger.WriteAsync(message, CancellationToken.None);
        }
    }

    private async Task WaitUntilReadyAsync(string path, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= _options.FileReadyRetryCount; attempt++)
        {
            try
            {
                await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
                if (stream.Length > 0)
                {
                    return;
                }
            }
            catch (IOException) when (attempt < _options.FileReadyRetryCount)
            {
            }

            await Task.Delay(_options.FileReadyRetryDelayMilliseconds, cancellationToken);
        }
    }

    private static async Task<IReadOnlyList<string>> ReadImageSourcesAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        var imageSources = await JsonSerializer.DeserializeAsync<List<string>>(stream, cancellationToken: cancellationToken);

        if (imageSources is null || imageSources.Count == 0)
        {
            throw new InvalidOperationException("JSON file must contain a non-empty array of image source strings.");
        }

        var cleanedSources = imageSources
            .Where(source => !string.IsNullOrWhiteSpace(source))
            .Select(source => source.Trim())
            .ToList();

        if (cleanedSources.Count == 0)
        {
            throw new InvalidOperationException("JSON file did not contain any usable image sources.");
        }

        return cleanedSources;
    }

    private static string GetUniquePath(string directory, string fileName)
    {
        var candidate = Path.Combine(directory, fileName);

        if (!File.Exists(candidate))
        {
            return candidate;
        }

        var name = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);

        for (var index = 1; ; index++)
        {
            candidate = Path.Combine(directory, $"{name}-{index}{extension}");

            if (!File.Exists(candidate))
            {
                return candidate;
            }
        }
    }

    private static string ResolvePath(string contentRootPath, string path)
    {
        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(contentRootPath, path));
    }

    private sealed record FileImportRequest(string Path, bool IsNsfw);
}
