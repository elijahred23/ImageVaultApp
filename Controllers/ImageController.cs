using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

using ImageVaultApp.Data;
using ImageVaultApp.ViewModels;




[Authorize]
public class ImageController: Controller
{
    private readonly ImageVaultDbContext _vaultContext;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public ImageController(
        ImageVaultDbContext vaultContext,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _vaultContext = vaultContext;
        _configuration = configuration;
        _environment = environment;
    }
    [HttpGet]
    public IActionResult Upload()
    {
        return View();
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(ImageUploadViewModel model)
    {
        var imageUrls = new List<string>();

        if (!string.IsNullOrWhiteSpace(model.ImageUrlsJson))
        {
            if (!TryParseImageUrls(model.ImageUrlsJson, out imageUrls, out var validationError))
            {
                ModelState.AddModelError(nameof(model.ImageUrlsJson), validationError);
                return View(model);
            }
        }
        else if (!string.IsNullOrWhiteSpace(model.ImageUrl))
        {
            imageUrls.Add(model.ImageUrl.Trim());
        }
        else if (model.File != null && model.File.Length > 0)
        {
            imageUrls.Add(await ConvertFileToDataUrlAsync(model.File));
        }
        else
        {
            ModelState.AddModelError(nameof(model.ImageUrlsJson), "Paste a JSON array containing at least one image URL.");
            return View(model);
        }

        int id = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var images = imageUrls.Select(imageUrl => new Image
        {
            UserId = id,
            Title = model.Title,
            Description = model.Description,
            IsNSFW = model.IsNSFW,
            MimeType = model.File?.ContentType,
            FileSizeBytes = model.File?.Length,
            ImageUrl = imageUrl,
        });

        _vaultContext.Images.AddRange(images);
        await _vaultContext.SaveChangesAsync();

        return RedirectToAction("Gallery");
    }

    [HttpGet]
    public IActionResult Import()
    {
        return View(CreateImportUploadViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(ImageImportUploadViewModel model, CancellationToken cancellationToken)
    {
        if (model.JsonFiles.Count == 0)
        {
            ModelState.AddModelError(nameof(model.JsonFiles), "Select at least one JSON file to import.");
            return View(CreateImportUploadViewModel(model));
        }

        var acceptedCount = 0;
        var dropFolder = GetImportDropFolder(model.IsNSFW);
        Directory.CreateDirectory(dropFolder);

        foreach (var file in model.JsonFiles.Where(file => file.Length > 0))
        {
            if (!Path.GetExtension(file.FileName).Equals(".json", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(model.JsonFiles), $"{file.FileName} is not a JSON file.");
                continue;
            }

            await using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;

            if (!await ContainsImageSourceArrayAsync(memoryStream, cancellationToken))
            {
                ModelState.AddModelError(nameof(model.JsonFiles), $"{file.FileName} must contain a non-empty JSON array of image source strings.");
                continue;
            }

            memoryStream.Position = 0;
            var importPath = GetUniqueImportPath(dropFolder, file.FileName);
            await using var output = new FileStream(importPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await memoryStream.CopyToAsync(output, cancellationToken);
            acceptedCount++;
        }

        foreach (var file in model.JsonFiles.Where(file => file.Length == 0))
        {
            ModelState.AddModelError(nameof(model.JsonFiles), $"{file.FileName} is empty.");
        }

        model.ImportedFileCount = acceptedCount;

        if (acceptedCount > 0)
        {
            model.StatusMessage = acceptedCount == 1
                ? "Queued 1 JSON file for import."
                : $"Queued {acceptedCount} JSON files for import.";
        }

        return View(CreateImportUploadViewModel(model));
    }

    [HttpGet]
    public async Task<IActionResult> Gallery(string searchTerm, int page = 1, int pageSize = 100)
    {
        const int defaultPageSize = 100;
        const int maxPageSize = 1000;

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = defaultPageSize;
        if (pageSize > maxPageSize) pageSize = maxPageSize;
        pageSize = Math.Clamp(((pageSize + 99) / 100) * 100, defaultPageSize, maxPageSize);

        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var settings = await _vaultContext.UserSettings
            .FirstOrDefaultAsync(s => s.UserId == userId);

        var query = _vaultContext.Images.Where(i => i.UserId == userId);

        if(settings?.AllowNSFW != true)
        {
            query = query.Where(i => !i.IsNSFW);
        }

        if(!string.IsNullOrWhiteSpace(searchTerm))
        {
            var trimmedSearchTerm = searchTerm.Trim();

            if (trimmedSearchTerm.Equals("nsfw", StringComparison.OrdinalIgnoreCase))
            {
                query = settings?.AllowNSFW == true
                    ? query.Where(i => i.IsNSFW)
                    : query.Where(i => false);
            }
            else
            {
                query = query.Where(i => (i.Title != null && i.Title.Contains(searchTerm)) || (i.Description != null && i.Description.Contains(searchTerm)));
            }

            ViewBag.SearchTerm = searchTerm;
        }

        var totalImages = await query.CountAsync();
        var totalPages = totalImages == 0 ? 1 : (int)Math.Ceiling(totalImages / (double)pageSize);

        if (page > totalPages) page = totalPages;

        var images = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        var favoriteImageIds = await _vaultContext.Favorites
            .Where(f => f.UserId == userId)
            .Select(f => f.ImageId)
            .ToListAsync();

        foreach (var image in images)
        {
            image.IsFavorited = favoriteImageIds.Contains(image.Id);
        }

        ViewBag.Settings = settings;
        ViewBag.FavoriteImageIds = favoriteImageIds;

        return View(new GalleryViewModel
        {
            Images = images,
            SearchTerm = searchTerm,
            Page = page,
            PageSize = pageSize,
            TotalImages = totalImages
        });
    }

    [HttpGet]
    public async Task<IActionResult> DownloadImageUrls(string searchTerm)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var settings = await _vaultContext.UserSettings
            .FirstOrDefaultAsync(s => s.UserId == userId);

        var query = _vaultContext.Images.Where(i => i.UserId == userId);

        if (settings?.AllowNSFW != true)
        {
            query = query.Where(i => !i.IsNSFW);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var trimmedSearchTerm = searchTerm.Trim();

            if (trimmedSearchTerm.Equals("nsfw", StringComparison.OrdinalIgnoreCase))
            {
                query = settings?.AllowNSFW == true
                    ? query.Where(i => i.IsNSFW)
                    : query.Where(i => false);
            }
            else
            {
                query = query.Where(i => (i.Title != null && i.Title.Contains(searchTerm)) || (i.Description != null && i.Description.Contains(searchTerm)));
            }
        }

        var imageUrls = await query
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => i.ImageUrl)
            .Where(imageUrl => !string.IsNullOrWhiteSpace(imageUrl))
            .ToListAsync();

        var json = JsonSerializer.SerializeToUtf8Bytes(imageUrls);
        return File(json, "application/json", "image-urls.json");
    }

    public async Task<IActionResult> Edit(int id)
    {
        var image = await _vaultContext.Images.FindAsync(id);

        if(image == null) return NotFound();

        if(image.UserId != int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value))
            return Forbid();

        var model = new ImageEditViewModel
        {
            Id = image.Id,
            Title = image.Title,
            Description = image.Description,
            IsNSFW = image.IsNSFW ,
            ExistingImagePath = image.ImageUrl
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ImageEditViewModel model)
    {
        var isAjaxRequest = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        if(!ModelState.IsValid)
        {
            if(isAjaxRequest)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Please correct the validation errors and try again."
                });
            }

            return View(model);
        }

        var image = await _vaultContext.Images.FindAsync(model.Id);


        if(image == null)
        {
            if(isAjaxRequest) return NotFound(new { success = false, message = "Image not found." });

            return NotFound();
        }

        if(image.UserId != int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value))
        {
            if(isAjaxRequest) return Forbid();

            return Forbid();    
        }

        image.Title = model.Title;

        image.Description = model.Description;

        image.IsNSFW = model.IsNSFW;

        if(model.NewFile != null && model.NewFile.Length > 0)
        {
            image.ImageUrl = await ConvertFileToDataUrlAsync(model.NewFile);
            image.MimeType = model.NewFile.ContentType;
            image.FileSizeBytes = model.NewFile.Length;
        }

        await _vaultContext.SaveChangesAsync();

        if(isAjaxRequest)
        {
            return Json(new
            {
                success = true,
                image = new
                {
                    id = image.Id,
                    title = image.Title,
                    description = image.Description,
                    isNSFW = image.IsNSFW,
                    imageUrl = image.ImageUrl
                }
            });
        }

        var referer = Request.Headers["Referer"].ToString();

        if(!string.IsNullOrEmpty(referer))
        {
            return Redirect(referer);
        }


        return RedirectToAction("Gallery");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var image = await _vaultContext.Images.FindAsync(id);

        var isAjaxRequest = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        if(image == null)
        {
            if(isAjaxRequest) return NotFound(new { success = false, message = "Image not found." });

            return NotFound();
        }

        if(image.UserId != int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value))
        {
            if(isAjaxRequest) return Forbid();

            return Forbid();
        }


        _vaultContext.Images.Remove(image);

        await _vaultContext.SaveChangesAsync();

        if(isAjaxRequest)
        {
            return Json(new { success = true, deletedImageId = id });
        }

        return RedirectToAction("Gallery");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleFavorite(int imageId)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        var imageExists = await _vaultContext.Images.AnyAsync(i => i.Id == imageId && i.UserId == userId);

        if(!imageExists) return NotFound();

        var favorite = await _vaultContext.Favorites.FirstOrDefaultAsync(f => f.UserId == userId && f.ImageId == imageId);

        bool isFavorited;

        if(favorite == null)
        {
            _vaultContext.Favorites.Add(new Favorite
            {
                UserId = userId,
                ImageId = imageId
            });
            isFavorited = true;
        }
        else
        {
            _vaultContext.Favorites.Remove(favorite);
            isFavorited = false;
        }

        await _vaultContext.SaveChangesAsync();

        return Json(new { success = true, isFavorited });   
    }
    [HttpGet]
    public async Task<IActionResult> Favorites(string searchTerm)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);


        var settings = await _vaultContext.UserSettings 
            .FirstOrDefaultAsync(s => s.UserId == userId);

        var query = _vaultContext.Images 
        .Where(i => i.UserId == userId && _vaultContext.Favorites.Any(f => f.ImageId == i.Id && f.UserId == userId));

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where( i => (i.Title != null && i.Title.Contains(searchTerm)) || (i.Description != null && i.Description.Contains(searchTerm)));
            ViewBag.SearchTerm = searchTerm;
        }

        var images = await query 
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        var favoriteImageIds = images.Select(i => i.Id).ToList();

        foreach(var img in images)
        {
            img.IsFavorited = true;
        }

        ViewBag.Settings = settings;

        ViewBag.FavoriteImageIds = favoriteImageIds;

        return View(images);
    }

    private static async Task<string> ConvertFileToDataUrlAsync(IFormFile file)
    {
        using var ms = new MemoryStream();

        await file.CopyToAsync(ms);

        var bytes = ms.ToArray();

        var base64 = Convert.ToBase64String(bytes);

        return $"data:{file.ContentType};base64,{base64}";
    }

    private static bool TryParseImageUrls(string json, out List<string> imageUrls, out string error)
    {
        const int maximumImageCount = 2000;
        imageUrls = [];
        error = string.Empty;

        try
        {
            using var document = JsonDocument.Parse(json);

            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                error = "The JSON must be an array of image URL strings.";
                return false;
            }

            foreach (var element in document.RootElement.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(element.GetString()))
                {
                    error = "Every array item must be a non-empty image URL string.";
                    return false;
                }

                var imageUrl = element.GetString()!.Trim();
                var isWebUrl = Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri)
                    && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

                if (!isWebUrl && !imageUrl.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
                {
                    error = $"'{imageUrl}' is not an HTTP, HTTPS, or image data URL.";
                    return false;
                }

                imageUrls.Add(imageUrl);

                if (imageUrls.Count > maximumImageCount)
                {
                    error = $"A single upload can contain at most {maximumImageCount} images.";
                    return false;
                }
            }

            if (imageUrls.Count == 0)
            {
                error = "The JSON array must contain at least one image URL.";
                return false;
            }

            return true;
        }
        catch (JsonException)
        {
            error = "The pasted value is not valid JSON.";
            return false;
        }
    }

    private ImageImportUploadViewModel CreateImportUploadViewModel(ImageImportUploadViewModel? model = null)
    {
        model ??= new ImageImportUploadViewModel();
        model.DropFolderPath = GetImportDropFolder(model.IsNSFW);
        return model;
    }

    private string GetImportDropFolder(bool isNsfw)
    {
        var dropRoot = _configuration["ImageImporter:DropRoot"];

        if (string.IsNullOrWhiteSpace(dropRoot))
        {
            dropRoot = Path.Combine(_environment.ContentRootPath, "src", "ImageVault.ImportService", "DropFolders");
        }
        else if (!Path.IsPathRooted(dropRoot))
        {
            dropRoot = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, dropRoot));
        }

        var folderName = isNsfw
            ? _configuration["ImageImporter:ProcessNsfwFolder"] ?? "process-nsfw"
            : _configuration["ImageImporter:ProcessFolder"] ?? "process";

        return Path.Combine(dropRoot, folderName);
    }

    private static async Task<bool> ContainsImageSourceArrayAsync(Stream stream, CancellationToken cancellationToken)
    {
        try
        {
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (json.RootElement.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            var hasImageSource = false;

            foreach (var element in json.RootElement.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.String)
                {
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(element.GetString()))
                {
                    hasImageSource = true;
                }
            }

            return hasImageSource;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string GetUniqueImportPath(string directory, string originalFileName)
    {
        var baseName = SanitizeFileName(Path.GetFileNameWithoutExtension(originalFileName));

        if (string.IsNullOrWhiteSpace(baseName))
        {
            baseName = "image-import";
        }

        var fileName = $"{baseName}-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}.json";
        return Path.Combine(directory, fileName);
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Concat(fileName.Select(character => invalidChars.Contains(character) ? '-' : character));
    }
}
