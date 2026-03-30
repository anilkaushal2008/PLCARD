using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PLCARD.Data;
using PLCARD.Models;
using ClosedXML.Excel;

namespace PLCARD.Pages.Corporates;

public class IndexModel(PLCARDContext context) : PageModel
{
    // Data list
    public IList<TblCompanyRegistration> Companies { get; set; } = default!;

    // Dashboard Statistics
    public int TotalCompanies { get; set; }
    public int ActiveAgreements { get; set; }
    public int CreditEnabled { get; set; }

    // Filters
    [BindProperty(SupportsGet = true)]
    public DateTime? FromDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? ToDate { get; set; }

    public async Task OnGetAsync()
    {
        var query = context.TblCompanyRegistration
            .Include(c => c.IntPlan)
            .AsNoTracking()
            .AsQueryable();

        // Apply filters if provided
        if (FromDate.HasValue)
            query = query.Where(c => c.DtCreated >= FromDate.Value);

        if (ToDate.HasValue)
            query = query.Where(c => c.DtCreated < ToDate.Value.AddDays(1));

        Companies = await query.OrderBy(c => c.VchCompanyName).ToListAsync();

        // Calculate Stats for the top cards
        TotalCompanies = Companies.Count;
        ActiveAgreements = Companies.Count(x => x.DtAgreementEnd.HasValue && x.DtAgreementEnd.Value.Date >= DateTime.Today);
        CreditEnabled = Companies.Count(x => x.BitIsCredit == true);
    }

    public async Task<IActionResult> OnPostExportExcelAsync()
    {
        var query = context.TblCompanyRegistration.Include(c => c.IntPlan).AsQueryable();

        if (FromDate.HasValue) query = query.Where(c => c.DtCreated >= FromDate.Value);
        if (ToDate.HasValue) query = query.Where(c => c.DtCreated < ToDate.Value.AddDays(1));

        var data = await query.OrderBy(c => c.VchCompanyName).ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Corporate_Audit");

        // Headers
        string[] headers = { "Company Name", "City", "Plan", "Contact Person", "Mobile", "GST No", "Agreement End", "Status" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E0E7FF");
        }

        int row = 1;
        foreach (var c in data)
        {
            row++;
            worksheet.Cell(row, 1).Value = c.VchCompanyName;
            worksheet.Cell(row, 2).Value = c.VchCity;
            worksheet.Cell(row, 3).Value = c.IntPlan?.VchPlanName;
            worksheet.Cell(row, 4).Value = c.VchContactPerson;
            worksheet.Cell(row, 5).Value = c.VchContactNo;
            worksheet.Cell(row, 6).Value = c.VchGstNo;
            worksheet.Cell(row, 7).Value = c.DtAgreementEnd?.ToString("dd-MMM-yyyy");
            worksheet.Cell(row, 8).Value = c.BitIsCredit ? "Credit Enabled" : "Cash Only";
        }

        worksheet.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Corporate_Directory_{DateTime.Now:yyyyMMdd}.xlsx");
    }
}