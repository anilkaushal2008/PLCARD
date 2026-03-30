using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PLCARD.Data;
using PLCARD.Models;
using ClosedXML.Excel;

namespace PLCARD.Pages.Cards;

public class IndexModel(PLCARDContext context) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public DateTime? FromDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? ToDate { get; set; }

    public IList<TblCardRegistration> CardRecords { get; set; } = default!;

    public async Task OnGetAsync()
    {
        var query = context.TblCardRegistration
            .Include(c => c.IntCard)
            .AsQueryable();

        if (FromDate.HasValue)
            query = query.Where(c => c.Dtcreated >= FromDate.Value);

        if (ToDate.HasValue)
            query = query.Where(c => c.Dtcreated < ToDate.Value.AddDays(1));

        CardRecords = await query.OrderByDescending(c => c.Dtcreated).ToListAsync();
    }

    public async Task<IActionResult> OnPostExportExcelAsync()
    {
        var query = context.TblCardRegistration
            .Include(c => c.IntCard)
            .AsQueryable();

        // Use the bound properties directly
        if (FromDate.HasValue) query = query.Where(c => c.Dtcreated >= FromDate.Value);
        if (ToDate.HasValue) query = query.Where(c => c.Dtcreated < ToDate.Value.AddDays(1));

        var data = await query.OrderByDescending(c => c.Dtcreated).ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Card Records");

        // Define Headers
        string[] headers = { "Issue Date", "UHID No", "Patient Name", "Card Type", "Contact No", "Gender", "Age", "Amount" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightBlue;
        }

        int currentRow = 1;
        foreach (var item in data)
        {
            currentRow++;
            worksheet.Cell(currentRow, 1).Value = item.Dtcreated?.ToString("dd-MMM-yyyy");
            worksheet.Cell(currentRow, 2).Value = item.VchUhidno;
            worksheet.Cell(currentRow, 3).Value = item.Vchname;
            worksheet.Cell(currentRow, 4).Value = item.VchCardType ?? (item.IntCard?.Vchcname) ?? "N/A";
            worksheet.Cell(currentRow, 5).Value = item.Vchcontactno;
            worksheet.Cell(currentRow, 6).Value = item.Vchsex;
            worksheet.Cell(currentRow, 7).Value = item.Intage;
            worksheet.Cell(currentRow, 8).Value = item.IntCharges;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"CardAudit_{DateTime.Now:yyyyMMdd}.xlsx");
    }
}