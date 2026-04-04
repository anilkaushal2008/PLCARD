using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PLCARD.Data;
using PLCARD.Models;

namespace PLCARD.Pages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly PLCARDContext _context;

        public IndexModel(PLCARDContext context)
        {
            _context = context;
        }

        // --- Dashboard KPI Stats ---
        public int TotalCompanies { get; set; }
        public int TotalCardsIssued { get; set; }
        public int CardsIssuedToday { get; set; }
        public int PendingSyncCount { get; set; }

        // --- Data for Analytics & Tables ---
        public List<int> MonthlyRegistrationData { get; set; } = new();
        public List<TblCompanyRegistration> RecentCompanies { get; set; } = new();
        public TblCardRegistration? SearchResult { get; set; }

        public List<int> CardTypeDistribution { get; set; } = new();



        public async Task OnGetAsync(string? searchUhid)
        {
            // 1. Fetch KPI Totals
            TotalCompanies = await _context.TblCompanyRegistration.CountAsync();
            TotalCardsIssued = await _context.TblCardRegistration.CountAsync();

            // 2. Count cards issued/synced today
            CardsIssuedToday = await _context.TblCardRegistration
                .CountAsync(c => c.Dtcreated >= DateTime.Today);

            // 3. Count pending items in the Sync Queue
            PendingSyncCount = await _context.TblSyncQueue
                .CountAsync(x => x.BitProcessed != true);

            // 4. Calculate Registration Trend Data (Month by Month for Current Year)
            var currentYear = DateTime.Today.Year;
            var monthlyCounts = await _context.TblCompanyRegistration
                .Where(c => c.DtRegistration.HasValue && c.DtRegistration.Value.Year == currentYear)
                .GroupBy(c => c.DtRegistration.Value.Month)
                .Select(g => new { Month = g.Key, Count = g.Count() })
                .ToListAsync();

            // Initialize 12 months with 0
            MonthlyRegistrationData = Enumerable.Repeat(0, 12).ToList();
            foreach (var item in monthlyCounts)
            {
                // Month 1 (Jan) corresponds to Index 0
                MonthlyRegistrationData[item.Month - 1] = item.Count;
            }

            // 5. Fetch Top 5 Recent Companies for the Dashboard Table
            RecentCompanies = await _context.TblCompanyRegistration
                .OrderByDescending(c => c.DtRegistration)
                .Take(5)
                .ToListAsync();

            // Fetch raw counts from database
            var rawTiers = await _context.TblCardRegistration
                .Where(c => !string.IsNullOrEmpty(c.VchCardType))
                .GroupBy(c => c.VchCardType.Trim().ToUpper()) // Trim spaces and make Uppercase
                .Select(g => new { Tier = g.Key, Count = g.Count() })
                .ToListAsync();

            // Map them safely to the list
            CardTypeDistribution = new List<int>
{
    rawTiers.FirstOrDefault(t => t.Tier == "PLATINUM")?.Count ?? 0,
    rawTiers.FirstOrDefault(t => t.Tier == "GOLD")?.Count ?? 0,
    rawTiers.FirstOrDefault(t => t.Tier == "SILVER")?.Count ?? 0
};

            // 6. Handle UHID Verification Search (Triggers the Modal)
            if (!string.IsNullOrEmpty(searchUhid))
            {
                SearchResult = await _context.TblCardRegistration
                    .FirstOrDefaultAsync(m => m.VchUhidno == searchUhid);

                if (SearchResult == null)
                {
                    TempData["SearchError"] = "UHID not found in our records.";
                }
            }
        }

        public IActionResult OnPostSearch(string uhid)
        {
            if (string.IsNullOrWhiteSpace(uhid))
                return RedirectToPage();

            // Redirect to the same page with the search parameter to trigger the modal in OnGet
            return RedirectToPage(new { searchUhid = uhid.Trim() });
        }
    }
}