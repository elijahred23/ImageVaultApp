using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

using ImageVaultApp.Data;
using ImageVaultApp.ViewModels;
using ImageVaultApp.Models;




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
        if(model.File == null || model.File.Length == 0)
        {
            ModelState.AddModelError("", "Please selecta file.");
            return View(model);
        }

        using var memoryStream = new MemoryStream();

        await model.File.CopyToAsync(memoryStream);

        var fileBytes = memoryStream.ToArray();

        var base64 = Convert.ToBase64String(fileBytes);

        var image = new Image
        {
            UserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value),
            Title = model.Title,
            Description = model.Description,
            IsNSFW = model.IsNSFW,
            MimeType = model.File.ContentType,
            FileSizeBytes = model.File.Length,
            ImageUrl = $"data:{model.File.ContentType};base64,{base64}"
        };

        _vaultContext.Images.Add(image);
        await _vaultContext.SaveChangesAsync();

        return RedirectToAction("Gallery");
    }

    [HttpGet]
    public async Task<IActionResult> Gallery()
    {
        var images = await _vaultContext.Images
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        return View(images);
    }
}