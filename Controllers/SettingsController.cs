using ImageVaultApp.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;
using System.Security.Claims;

[Authorize]
public class SettingsController : Controller
{
    private readonly ImageVaultDbContext _context;

    public SettingsController(ImageVaultDbContext context)
    {
        _context = context;
    }

    private int GetUserId()
    {
        return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = GetUserId();

        var settings = await _context.UserSettings 
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (settings == null)
        {
            settings = new UserSettings
            {
                UserId = userId
            };

            _context.UserSettings.Add(settings);

            await _context.SaveChangesAsync();
        }

        var vm = new UserSettingsViewModel
        {
            AllowNSFW = settings.AllowNSFW,
            BlurNSFW = settings.BlurNSFW
        };

        return View(vm);
    }


    [HttpPost]
    public async Task<IActionResult> Index(UserSettingsViewModel model)
    {
        var userId = GetUserId();

        var settings = await _context.UserSettings
            .FirstOrDefaultAsync(s => s.UserId == userId); 


        if (settings == null)
        {
            return NotFound();
        }


        settings.AllowNSFW = model.AllowNSFW;
        settings.BlurNSFW = model.BlurNSFW;

        await _context.SaveChangesAsync();


        ViewBag.Message = "Settings saved!";

        return View(model);
    }
    
}