using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PLCARD.Data;
using PLCARD.Models;

namespace PLCARD.Pages.Cards;

public class IndexModel(PLCARDContext context) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public DateTime? FromDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? ToDate { get; set; }

    public IList<CardRegistrationReportResult> CardRecords { get; set; } = new List<CardRegistrationReportResult>();

    public async Task OnGetAsync()
    {
        object fromParam = (object)FromDate ?? DBNull.Value;
        object toParam = (object)ToDate ?? DBNull.Value;

        CardRecords = await context.Database
            .SqlQueryRaw<CardRegistrationReportResult>(
                "EXEC [dbo].[sp_GetCardRegistrationReport] @FromDate={0}, @ToDate={1}",
                fromParam, toParam)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostExportExcelAsync()
    {
        // To export ALL cards, we pass NULL to both date parameters 
        // effectively bypassing the 3-month default in the SP
        var data = await context.Database
            .SqlQueryRaw<CardRegistrationReportResult>(
                "EXEC [dbo].[sp_GetCardRegistrationReport] @FromDate={0}, @ToDate={1}",
                DBNull.Value, DBNull.Value)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Full Card Audit");

        string[] headers = { "Ref ID", "Date", "UHID", "Name", "Card Type", "Contact", "City", "Charges" };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
        }

        int row = 2;
        foreach (var item in data)
        {
            worksheet.Cell(row, 1).Value = item.IntRegId;
            worksheet.Cell(row, 2).Value = item.Dtcreated?.ToString("dd-MMM-yyyy");
            worksheet.Cell(row, 3).Value = item.VchUhidno;
            worksheet.Cell(row, 4).Value = item.Vchname;
            worksheet.Cell(row, 5).Value = item.VchCardType;
            worksheet.Cell(row, 6).Value = item.Vchcontactno;
            worksheet.Cell(row, 7).Value = item.Vchcity;
            worksheet.Cell(row, 8).Value = item.IntCharges;
            row++;
        }

        worksheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Full_Card_Report.xlsx");
    }
}