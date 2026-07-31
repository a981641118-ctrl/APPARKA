using ApparkaTrainingFlowOnline.Data;
using ApparkaTrainingFlowOnline.Models;
using ApparkaTrainingFlowOnline.Services;
using ApparkaTrainingFlowOnline.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ApparkaTrainingFlowOnline.Controllers;

public class AccountController(AppDbContext db, PasswordService passwords, AuditService audit) : Controller
{
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("sensitive")]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var email = model.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == email && x.IsActive);
        if (user is null || !passwords.Verify(user, model.Password))
        {
            ModelState.AddModelError(string.Empty, "Correo o contraseña incorrectos.");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role)
        };
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
        await audit.WriteAsync("LOGIN", nameof(AppUser), user.Id, "Inicio de sesión correcto.");
        return LocalRedirect(string.IsNullOrWhiteSpace(model.ReturnUrl) ? "/" : model.ReturnUrl);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Activate(string token)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.ActivationToken == token && x.ActivationExpiresAt > DateTimeOffset.UtcNow);
        if (user is null) return View("ActivationInvalid");
        return View(new ActivateViewModel { Token = token });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(ActivateViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await db.Users.SingleOrDefaultAsync(x => x.ActivationToken == model.Token && x.ActivationExpiresAt > DateTimeOffset.UtcNow);
        if (user is null) return View("ActivationInvalid");
        user.PasswordHash = passwords.Hash(user, model.Password);
        user.ActivationToken = null;
        user.ActivationExpiresAt = null;
        user.MustChangePassword = false;
        await db.SaveChangesAsync();
        TempData["Success"] = "Cuenta activada. Ya puedes iniciar sesión.";
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();
}
