using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

using ImageVaultApp.Data;
using ImageVaultApp.ViewModels;
using ImageVaultApp.Models;
using Microsoft.AspNetCore.Components.Web;
using System.ComponentModel;




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
    public async Task<IActionResult> Gallery()
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var settings = await _vaultContext.UserSettings
            .FirstOrDefaultAsync(s => s.UserId == userId);

        var images = await _vaultContext.Images
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        ViewBag.Settings = settings;

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


        return RedirectToAction("Gallery");
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var image = await _vaultContext.Images.FindAsync(id);

        if(image == null) NotFound();

        if(image.UserId != int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value))
            return Forbid();


        _vaultContext.Images.Remove(image);

        await _vaultContext.SaveChangesAsync();

        return RedirectToAction("Gallery");
    }
}