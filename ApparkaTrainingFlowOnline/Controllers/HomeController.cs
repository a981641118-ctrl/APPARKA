using ApparkaTrainingFlowOnline.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApparkaTrainingFlowOnline.Controllers;

[Authorize]
public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (User.IsInRole(AppRoles.Collaborator)) return RedirectToAction("Index", "Training");
        if (User.IsInRole(AppRoles.Supervisor)) return RedirectToAction("Dashboard", "Supervisor");
        if (User.IsInRole(AppRoles.Hr)) return RedirectToAction("Index", "Hr");
        return RedirectToAction("Index", "Admin");
    }

    [AllowAnonymous]
    public IActionResult Error() => View();
}
