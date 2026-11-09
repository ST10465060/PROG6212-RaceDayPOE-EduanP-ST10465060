using Microsoft.AspNetCore.Mvc;
using RaceDay.Contracts.Events;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>
/// The participant's window onto the system. Deliberately not locked down
/// at the class level, browsing races is something a visitor should be
/// able to do before they decide to sign up.
/// </summary>
public class RacesController : Controller
{
    private readonly ApiClient _api;

    public RacesController(ApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? type)
    {
        // Only Open events belong on the public list.
        var events = await _api.GetListAsync<EventSummaryDto>("api/events?status=Open");

        // Filtering happens here rather than in the API because the list is small 
        if (!string.IsNullOrWhiteSpace(search))
        {
            events = events
                .Where(e => e.EventName.Contains(search, StringComparison.OrdinalIgnoreCase)
                         || e.City.Contains(search, StringComparison.OrdinalIgnoreCase)
                         || e.VenueName.Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            events = events.Where(e => e.EventType == type).ToList();
        }

        // Soonest first, that is what someone looking for a race wants.
        events = events.OrderBy(e => e.EventDate).ToList();

        // Hand the current filters back so the form stays filled in.
        ViewData["Search"] = search;
        ViewData["Type"] = type;

        return View(events);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var detail = await _api.GetAsync<EventDetailDto>($"api/events/{id}");

        if (detail is null)
        {
            TempData["Error"] = "That race could not be found.";
            return RedirectToAction(nameof(Index));
        }

        return View(detail);
    }
}