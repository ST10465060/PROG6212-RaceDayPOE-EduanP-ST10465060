using Microsoft.AspNetCore.Mvc;
using RaceDay.Contracts.Auth;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class AccountController : Controller
{
    private readonly ApiClient _api;

    public AccountController(ApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public IActionResult Register()
    {
        // Already signed in? No reason to be on this page.
        if (HttpContext.Session.IsLoggedIn())
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new RegisterRequest());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        // The contracts carry their own data annotations, so the same rules
        // run here in the browser and again inside the API. Belt and braces.
        if (!ModelState.IsValid)
        {
            return View(request);
        }

        // Under 16s cannot enter a race unsupervised, and the API has no
        // view of "today" at validation time, so the check lives here.
        var age = DateTime.Today.Year - request.DateOfBirth.Year;
        if (request.DateOfBirth > DateOnly.FromDateTime(DateTime.Today.AddYears(-age)))
        {
            age--;
        }

        if (age < 16)
        {
            ModelState.AddModelError(nameof(request.DateOfBirth),
                "You must be at least 16 years old to register.");
            return View(request);
        }

        var result = await _api.PostAsync<RegisterRequest, AuthUserDto>(
            "api/auth/register", request);

        if (!result.Success || result.Data is null)
        {
            // Most likely the email is already taken. Show the API's own
            // wording rather than inventing a vaguer message here.
            ModelState.AddModelError(string.Empty,
                result.ErrorMessage ?? "Registration failed. Please try again.");
            return View(request);
        }

        // Straight in after registering, no need to make them log in twice.
        HttpContext.Session.SignIn(result.Data);

        TempData["Success"] = $"Welcome to RaceDay, {result.Data.FirstName}.";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (HttpContext.Session.IsLoggedIn())
        {
            return RedirectToAction("Index", "Home");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginRequest());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginRequest request, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(request);
        }

        var result = await _api.PostAsync<LoginRequest, AuthUserDto>(
            "api/auth/login", request);

        if (!result.Success || result.Data is null)
        {
            // Deliberately vague. Saying "no such email" would let someone
            // work out which addresses are registered on the system.
            ModelState.AddModelError(string.Empty,
                "That email address and password combination is not correct.");
            return View(request);
        }

        HttpContext.Session.SignIn(result.Data);

        // Only follow a local URL. An open redirect would let an attacker
        // send our users to a lookalike site after a genuine login.
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        // Clear the lot. Removing individual keys risks leaving something
        // behind that makes the next user look partially signed in.
        HttpContext.Session.Clear();

        TempData["Success"] = "You have been signed out.";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        ViewData["Role"] = HttpContext.Session.GetUserRole();
        return View();
    }
}