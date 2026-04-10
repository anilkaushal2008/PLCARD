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

        // --- Dashboard KPI Stats (Matched to HTML) ---
        public int TotalCompanies { get; set; }
        public int TodayRegistrations { get; set; } // Added for the new card
        public int TotalCards { get; set; }        // Renamed to match HTML @Model.TotalCards
        public int TodayIssuedCards { get; set; }  // Renamed to match HTML @Model.TodayIssuedCards
        public int PendingSyncCount { get; set; }

        // --- Data for Analytics & Tables ---
        public List<int> MonthlyRegistrationData { get; set; } = new();
        public List<TblCompanyRegistration> RecentCompanies { get; set; } = new();
        public TblCardRegistration? SearchResult { get; set; }
        public List<int> CardTypeDistribution { get; set; } = new();

        public async Task OnGetAsync(string? searchUhid)
        {
            // 1. Fetch KPI Totals directly from DB
            TotalCompanies = await _context.TblCompanyRegistration.CountAsync();
            TotalCards = await _context.TblCardRegistration.CountAsync();

            // 2. Today's Stats
            var today = DateTime.Today;

            // Companies registered today
            TodayRegistrations = await _context.TblCompanyRegistration
                .CountAsync(c => c.DtRegistration >= today);

            // Cards issued today
            TodayIssuedCards = await _context.TblCardRegistration
                .CountAsync(c => c.Dtcreated >= today);

            // 3. Count pending items in the Sync Queue
            PendingSyncCount = await _context.TblSyncQueue
                .CountAsync(x => x.BitProcessed != true);

            // 4. Calculate Registration Trend (Current Year Only)
            var currentYear = DateTime.Today.Year;
            var monthlyCounts = await _context.TblCompanyRegistration
                .Where(c => c.DtRegistration.HasValue && c.DtRegistration.Value.Year == currentYear)
                .GroupBy(c => c.DtRegistration.Value.Month)
                .Select(g => new { Month = g.Key, Count = g.Count() })
                .ToListAsync();

            MonthlyRegistrationData = Enumerable.Repeat(0, 12).ToList();
            foreach (var item in monthlyCounts)
            {
                MonthlyRegistrationData[item.Month - 1] = item.Count;
            }

            // 5. Fetch Top 5 Recent Companies
            RecentCompanies = await _context.TblCompanyRegistration
                .OrderByDescending(c => c.DtRegistration)
                .Take(5)
                .ToListAsync();

            // 6. Card Type Distribution (For the Doughnut Chart)
            var rawTiers = await _context.TblCardRegistration
                .Where(c => !string.IsNullOrEmpty(c.VchCardType))
                .GroupBy(c => c.VchCardType.Trim().ToUpper())
                .Select(g => new { Tier = g.Key, Count = g.Count() })
                .ToListAsync();

            CardTypeDistribution = new List<int>
            {
                rawTiers.FirstOrDefault(t => t.Tier == "PLATINUM")?.Count ?? 0,
                rawTiers.FirstOrDefault(t => t.Tier == "GOLD")?.Count ?? 0,
                rawTiers.FirstOrDefault(t => t.Tier == "SILVER")?.Count ?? 0
            };

            // 7. UHID Search Logic
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

            return RedirectToPage(new { searchUhid = uhid.Trim() });
        }
    }
}