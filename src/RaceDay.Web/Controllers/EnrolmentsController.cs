using Microsoft.AspNetCore.Mvc;
using RaceDay.Contracts.Enrolments;
using RaceDay.Web.Filters;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>
/// Entering races and seeing what you have entered. Participants only,
/// an organiser has no entries of their own under this account.
/// </summary>
[RequireRole("Participant")]
public class EnrolmentsController : Controller
{
    private readonly ApiClient _api;

    public EnrolmentsController(ApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public async Task<IActionResult> MyRaces()
    {
        var participantId = HttpContext.Session.GetUserId()!.Value;

        var entries = await _api.GetListAsync<MyEnrolmentDto>(
            $"api/enrolments/participant/{participantId}");

        // Next race first, past races underneath.
        entries = entries.OrderBy(e => e.EventDate).ToList();

        return View(entries);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enrol(int categoryId, int eventId)
    {
        // Same rule as creating an event. The participant is whoever is
        // signed in, never a value posted from the browser, otherwise one
        // person could enter a race on somebody else's behalf.
        var request = new EnrolRequest
        {
            CategoryId = categoryId,
            ParticipantId = HttpContext.Session.GetUserId()!.Value
        };

        var result = await _api.PostAsync<EnrolRequest, MyEnrolmentDto>(
            "api/enrolments", request);

        if (!result.Success)
        {
            // The API owns the real rules here, full category, duplicate entry, event not open. Show its wording rather than guessing.
            TempData["Error"] = result.ErrorMessage ?? "You could not be entered into this race.";
            return RedirectToAction("Details", "Races", new { id = eventId });
        }

        TempData["Success"] = "You are entered. Good luck.";
        return RedirectToAction(nameof(MyRaces));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Withdraw(int id)
    {
        var result = await _api.DeleteAsync($"api/enrolments/{id}");

        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage ?? "Your entry could not be withdrawn.";
        }
        else
        {
            TempData["Success"] = "You have been withdrawn from that race.";
        }

        return RedirectToAction(nameof(MyRaces));
    }
}