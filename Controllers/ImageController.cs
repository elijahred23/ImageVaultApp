using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

using ImageVaultApp.Data;
using ImageVaultApp.ViewModels;




[Authorize]
public class ImageController: Controller
{
    private readonly ImageVaultDbContext _vaultContext;

    public ImageController(ImageVaultDbContext vaultContext)
    {
        _vaultContext = vaultContext;
    }
    [HttpGet]
    public IActionResult Upload()
    {
        return View();
    }
    [HttpPost]
    public async Task<IActionResult> Upload(ImageUploadViewModel model)
    {
        string finalImageUrl;

        if (!string.IsNullOrWhiteSpace(model.ImageUrl))
        {
            finalImageUrl = model.ImageUrl;
        }
        else if (model.File != null && model.File.Length > 0)
        {
            using var ms = new MemoryStream();

            await model.File.CopyToAsync(ms);

            var bytes = ms.ToArray();

            var base64 = Convert.ToBase64String(bytes);

            finalImageUrl = $"data:{model.File.ContentType};base64,{base64}";
        } else
        {
            ModelState.AddModelError("", "Please select a file.");
            return View(model);
        }

        int id = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var image = new Image
        {
            UserId = id,
            Title = model.Title,
            Description = model.Description,
            IsNSFW = model.IsNSFW,
            MimeType = model.File?.ContentType,
            FileSizeBytes = model.File?.Length,
            ImageUrl = finalImageUrl,
        };

        _vaultContext.Images.Add(image);
        await _vaultContext.SaveChangesAsync();

        return RedirectToAction("Gallery");
    }

    [HttpGet]
    public async Task<IActionResult> Gallery(string searchTerm)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var settings = await _vaultContext.UserSettings
            .FirstOrDefaultAsync(s => s.UserId == userId);

        var query = _vaultContext.Images.Where(i => i.UserId == userId);


        if(!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(i => (i.Title != null && i.Title.Contains(searchTerm)) || (i.Description != null && i.Description.Contains(searchTerm)));
            ViewBag.SearchTerm = searchTerm;
        }


        var images = await query
            .OrderByDescending(i => i.CreatedAt)
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

        return View(images);
    }
    public async Task<IActionResult> Edit(int id)
    {
        var image = await _vaultContext.Images.FindAsync(id);

        if(image == null) return NotFound();


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
    public async Task<IActionResult> Edit(ImageEditViewModel model)
    {
        if(!ModelState.IsValid)
            return View(model);

        var image = await _vaultContext.Images.FindAsync(model.Id);


        if(image == null) return NotFound();

        if(image.UserId != int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value))
            return Forbid();    

        image.Title = model.Title;

        image.Description = model.Description;

        image.IsNSFW = model.IsNSFW;

        await _vaultContext.SaveChangesAsync();

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
}
