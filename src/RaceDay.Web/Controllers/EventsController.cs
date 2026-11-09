using Microsoft.AspNetCore.Mvc;
using RaceDay.Contracts.Events;
using RaceDay.Contracts.Venues;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>
/// Everything an organiser does with their own events. The whole controller
/// is locked to the Organiser role, a participant browsing events uses a
/// different controller entirely.
/// </summary>
[RequireRole("Organiser")]
public class EventsController : Controller
{
    private readonly ApiClient _api;

    public EventsController(ApiClient api)
    {
        _api = api;
    }

    // The organiser's own list of events.
    [HttpGet]
    public async Task<IActionResult> Manage()
    {
        // The filter already guaranteed we are signed in, so the id is safe
        // to read, but the compiler does not know that.
        var organiserId = HttpContext.Session.GetUserId()!.Value;

        // GetListAsync hands back an empty list rather than null, so the
        // view can loop over it without a guard.
        var events = await _api.GetListAsync<EventSummaryDto>(
            $"api/events?organiserId={organiserId}");

        return View(events);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new EventFormViewModel
        {
            Venues = await _api.GetListAsync<VenueOptionDto>("api/venues/options"),
            Event = new UpdateEventRequest
            {
                // Sensible defaults so the form does not open on 0001-01-01.
                EventDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(1)),
                Status = "Draft"
            }
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EventFormViewModel model)
    {
        // The organiser is whoever is signed in, never whatever the form
        // posted. Trusting a hidden field here would let one organiser
        // create events under another organiser's name.
        model.Event.OrganiserId = HttpContext.Session.GetUserId()!.Value;

        // OrganiserId was validated before the line above ran, so clear the
        // stale model state entry before checking validity.
        ModelState.Remove("Event.OrganiserId");

        if (model.Event.EventDate <= DateOnly.FromDateTime(DateTime.Today))
        {
            ModelState.AddModelError("Event.EventDate",
                "The event date must be in the future.");
        }

        if (!ModelState.IsValid)
        {
            // Repopulate the dropdown, it does not survive the round trip.
            model.Venues = await _api.GetListAsync<VenueOptionDto>("api/venues/options");
            return View(model);
        }

        // Map across to the create contract, which has no Status field
        // because a new event always starts life as a Draft.
        var request = new CreateEventRequest
        {
            EventName = model.Event.EventName,
            Description = model.Event.Description,
            EventDate = model.Event.EventDate,
            DistanceKm = model.Event.DistanceKm,
            EventType = model.Event.EventType,
            VenueId = model.Event.VenueId,
            OrganiserId = model.Event.OrganiserId
        };

        var result = await _api.PostAsync<CreateEventRequest, EventDetailDto>(
            "api/events", request);

        if (!result.Success || result.Data is null)
        {
            ModelState.AddModelError(string.Empty,
                result.ErrorMessage ?? "The event could not be created.");

            model.Venues = await _api.GetListAsync<VenueOptionDto>("api/venues/options");
            return View(model);
        }

        TempData["Success"] = $"\"{result.Data.EventName}\" has been created as a draft.";
        return RedirectToAction(nameof(Manage));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        // GetAsync returns the object itself, or null if the API said 404
        // or could not be reached at all.
        var existing = await _api.GetAsync<EventDetailDto>($"api/events/{id}");

        if (existing is null)
        {
            TempData["Error"] = "That event could not be found.";
            return RedirectToAction(nameof(Manage));
        }

        var model = new EventFormViewModel
        {
            EventId = id,
            Venues = await _api.GetListAsync<VenueOptionDto>("api/venues/options"),
            Event = new UpdateEventRequest
            {
                EventName = existing.EventName,
                Description = existing.Description,
                EventDate = existing.EventDate,
                DistanceKm = existing.DistanceKm,
                EventType = existing.EventType,
                Status = existing.Status,
                OrganiserId = HttpContext.Session.GetUserId()!.Value
            }
        };

        // EventDetailDto carries the venue name for display, not the id, so
        // match it back to the dropdown to preselect the right option.
        var venue = model.Venues.FirstOrDefault(
            v => v.DisplayName.StartsWith(existing.VenueName));

        if (venue is not null)
        {
            model.Event.VenueId = venue.VenueId;
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EventFormViewModel model)
    {
        model.EventId = id;
        model.Event.OrganiserId = HttpContext.Session.GetUserId()!.Value;
        ModelState.Remove("Event.OrganiserId");

        if (!ModelState.IsValid)
        {
            model.Venues = await _api.GetListAsync<VenueOptionDto>("api/venues/options");
            return View(model);
        }

        var result = await _api.PutAsync($"api/events/{id}", model.Event);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty,
                result.ErrorMessage ?? "The event could not be updated.");

            model.Venues = await _api.GetListAsync<VenueOptionDto>("api/venues/options");
            return View(model);
        }

        TempData["Success"] = "The event has been updated.";
        return RedirectToAction(nameof(Manage));
    }

    // The organiser's view of one event, including its categories.
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var detail = await _api.GetAsync<EventDetailDto>($"api/events/{id}");

        if (detail is null)
        {
            TempData["Error"] = "That event could not be found.";
            return RedirectToAction(nameof(Manage));
        }

        return View(detail);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var organiserId = HttpContext.Session.GetUserId()!.Value;

        // The API checks ownership itself and returns 403 if this organiser
        // does not own the event, so the id has to travel with the request.
        var result = await _api.DeleteAsync($"api/events/{id}?organiserId={organiserId}");

        if (!result.Success)
        {
            // Covers the 409 for an event that already has entrants.
            TempData["Error"] = result.ErrorMessage ?? "The event could not be deleted.";
        }
        else
        {
            TempData["Success"] = "The event has been deleted.";
        }

        return RedirectToAction(nameof(Manage));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadBanner(int id, IFormFile? banner)
    {
        if (banner is null || banner.Length == 0)
        {
            TempData["Error"] = "Please choose an image before uploading.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // The API validates type and size properly, this is just a quick
        // check so an obvious mistake does not cost a round trip.
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extension = Path.GetExtension(banner.FileName).ToLowerInvariant();

        if (!allowed.Contains(extension))
        {
            TempData["Error"] = "Only JPG, PNG or WEBP images are allowed.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // OpenReadStream gives a stream over the uploaded file without
        // loading the whole thing into memory first.
        await using var stream = banner.OpenReadStream();

        var result = await _api.PostFileAsync(
            $"api/events/{id}/banner", stream, banner.FileName, banner.ContentType);

        TempData[result.Success ? "Success" : "Error"] = result.Success
            ? "The banner image has been uploaded."
            : result.ErrorMessage ?? "The image could not be uploaded.";

        return RedirectToAction(nameof(Details), new { id });
    }

}