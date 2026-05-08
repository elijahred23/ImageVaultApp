using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

using System.Security.Claims;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.AspNetCore.Identity;
using ImageVaultApp.Data;
using ImageVaultApp.Models;


public class AuthController : Controller
{
    private readonly UsersDbContext _usersContext;
    private readonly ImageVaultDbContext _vaultContext;


    public AuthController(UsersDbContext usersContext, ImageVaultDbContext vaultContext)
    {
        _usersContext = usersContext;
        _vaultContext = vaultContext;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string username , string password)
    {
        var user =await _usersContext.Users
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);


        if (user == null)
        {
            ModelState.AddModelError("", "Invalid username or password");

            return View();
        }

        var hasher = new PasswordHasher<string>();

        var result = hasher.VerifyHashedPassword(
            user.Username,
            user.PasswordHash,
            password
        );

        if(result == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError("", "Invalid username or password");

            return View();
        }


        var settings = await _vaultContext.UserSettings
            .FirstOrDefaultAsync(s => s.UserId == user.Id);

        if(settings == null)
        {
            settings = new UserSettings
            {
                UserId = user.Id,
                AllowNSFW = false,
                BlurNSFW = true
            };
        
            _vaultContext.UserSettings.Add(settings);
            await _vaultContext.SaveChangesAsync();
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,principal);
        
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction("Login", "Auth");
    }

    public IActionResult AccessDenied()
    {
        return View();
    }
    
}