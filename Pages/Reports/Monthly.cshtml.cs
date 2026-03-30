using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PLCARD.Data;
using PLCARD.Models;
using System.ComponentModel.DataAnnotations;

namespace PLCARD.Pages.Reports;

public class MonthlyModel(PLCARDContext context) : PageModel
{
    [BindProperty(SupportsGet = true)]
    [DataType(DataType.Date)]
    public DateTime? FromDate { get; set; }

    [BindProperty(SupportsGet = true)]
    [DataType(DataType.Date)]
    public DateTime? ToDate { get; set; }

    public List<string> Labels { get; set; } = new();
    public List<int> Data { get; set; } = new();

    public async Task OnGetAsync()
    {
        // 1. Set Defaults (if no dates are selected, show current month)
        FromDate ??= new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        ToDate ??= DateTime.Now;

        // 2. Query with the Date Filter
        // Note: Use .Date to ensure we compare only the day, not the time
        var reportData = await context.TblCardRegistration
            .Include(c => c.IntCardId)
            .Where(c => c.Dtcreated >= FromDate && c.Dtcreated <= ToDate)
            .GroupBy(c => c.VchCardType)
            .Select(g => new
            {
                PlanName = g.Key ?? "Unknown",
                Count = g.Count()
            })
            .ToListAsync();

        Labels = reportData.Select(x => x.PlanName).ToList();
        Data = reportData.Select(x => x.Count).ToList();
    }
}
