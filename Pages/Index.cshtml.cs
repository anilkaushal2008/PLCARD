using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PLCARD.Data;
using PLCARD.Models;

namespace PLCARD.Pages;

public class IndexModel(PLCARDContext context) : PageModel
{
    public int TotalCompanies { get; set; }
    public int TotalCardsIssued { get; set; }
    public int CardsIssuedToday { get; set; }

    // This will hold the result of our search
    public TblCardRegistration? SearchResult { get; set; }

    public async Task OnGetAsync(string? searchUhid)
    {
        // 1. Basic Stats
        TotalCompanies = await context.TblCompanyRegistration.CountAsync();
        TotalCardsIssued = await context.TblCardRegistration.CountAsync();

        var today = DateTime.Today;
        CardsIssuedToday = await context.TblCardRegistration
            .CountAsync(c => c.Dtcreated.HasValue && c.Dtcreated.Value.Date == today);

        // 2. Handle Search (if a UHID was passed via URL)
        if (!string.IsNullOrEmpty(searchUhid))
        {
            SearchResult = await context.TblCardRegistration               
                .FirstOrDefaultAsync(c => c.VchUhidno == searchUhid);

            if (SearchResult == null)
            {
                TempData["SearchError"] = $"No card found for UHID: {searchUhid}";
            }
        }
    }

    public IActionResult OnPostSearch(string uhid)
    {
        if (string.IsNullOrWhiteSpace(uhid)) return RedirectToPage();
        // Redirect to the same page with the search parameter in the URL
        return RedirectToPage(new { searchUhid = uhid });
    }
}
