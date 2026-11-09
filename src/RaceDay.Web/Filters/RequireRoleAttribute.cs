using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Web.Services;

namespace RaceDay.Web.Filters;

/// <summary>
/// Put this on a controller or an action to lock it down. Checking the
/// session inside every single action would mean the same six lines
/// copied twenty times, and the one you forget is the security hole.
/// </summary>
public class RequireRoleAttribute : ActionFilterAttribute
{
    private readonly string? _requiredRole;

    // No role given means "any signed in user will do".
    public RequireRoleAttribute(string? requiredRole = null)
    {
        _requiredRole = requiredRole;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var session = context.HttpContext.Session;

        if (!session.IsLoggedIn())
        {
            // Remember where they were headed so we can send them back
            // there once they have signed in.
            var returnUrl = context.HttpContext.Request.Path.ToString();

            context.Result = new RedirectToActionResult(
                "Login", "Account", new { returnUrl });
            return;
        }

        if (_requiredRole is not null && session.GetUserRole() != _requiredRole)
        {
            // Signed in, but wrong role. That is forbidden, not unauthorised,
            // so do not bounce them to the login page.
            context.Result = new RedirectToActionResult(
                "AccessDenied", "Account", null);
            return;
        }

        base.OnActionExecuting(context);
    }
}
