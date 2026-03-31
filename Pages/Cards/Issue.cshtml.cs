using ClosedXML.Excel;
using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PLCARD.Data;
using PLCARD.Models;

namespace PLCARD.Pages.Cards;

public class IssueModel(PLCARDContext context) : PageModel
{
    [BindProperty] public TblCardRegistration Registration { get; set; } = new();
    public SelectList CardTypeList { get; set; } = default!;
    public int? CurrentCost { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        await LoadData();
        if (id.HasValue && id > 0)
        {
            Registration = await context.TblCardRegistration.FindAsync(id.Value);
            if (Registration == null) return NotFound();
            CurrentCost = await context.TblCardMaster.Where(x => x.IntCard == Registration.IntCardId).Select(x => x.Intcostcard).FirstOrDefaultAsync();
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Registration.IntRegId == 0)
        {
            Registration.Dtcreated = DateTime.Now;
            Registration.Vchcreatedby = User.Identity?.Name ?? "Admin";
            context.Add(Registration);
        }
        else
        {
            context.Attach(Registration).State = EntityState.Modified;
        }
        await context.SaveChangesAsync();
        return RedirectToPage("./Index");
    }

    // DOWNLOAD TEMPLATE (Updated with 16 columns)
    public IActionResult OnGetDownloadTemplate()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Cards");
        string[] h = { "Name", "Mobile", "DOB", "Gender", "Age", "Spouse", "Email", "Address", "City", "State", "Pin", "UHID", "Receipt", "Amount", "CardName", "RefBy" };
        for (int i = 0; i < h.Length; i++) ws.Cell(1, i + 1).Value = h[i];
        ws.RangeUsed().Style.Font.Bold = true;
        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "CardTemplate.xlsx");
    }

    // PREVIEW EXCEL (Updated to read RefBy from Cell 16)
    public async Task<JsonResult> OnPostPreviewExcelAsync(IFormFile ExcelFile)
    {
        if (ExcelFile == null) return new JsonResult(new { error = "Select file" });
        var preview = new List<object>();
        var cardList = await context.TblCardMaster.ToListAsync();
        int total = 0;

        using var stream = new MemoryStream();
        await ExcelFile.CopyToAsync(stream);
        using var wb = new XLWorkbook(stream);
        var rows = wb.Worksheet(1).RangeUsed().RowsUsed().Skip(1);

        foreach (var row in rows)
        {
            var cardName = row.Cell(15).GetValue<string>()?.Trim();
            var amt = row.Cell(14).GetValue<int>();

            preview.Add(new
            {
                name = row.Cell(1).GetValue<string>(),
                mobile = row.Cell(2).GetValue<string>(),
                dob = row.Cell(3).GetValue<string>(),
                gender = row.Cell(4).GetValue<string>(),
                age = row.Cell(5).GetValue<string>(),
                spouse = row.Cell(6).GetValue<string>(),
                email = row.Cell(7).GetValue<string>(),
                address = row.Cell(8).GetValue<string>(),
                city = row.Cell(9).GetValue<string>(),
                state = row.Cell(10).GetValue<string>(),
                pincode = row.Cell(11).GetValue<string>(),
                uhid = row.Cell(12).GetValue<string>(),
                receipt = row.Cell(13).GetValue<string>(),
                amount = amt,
                cardName = cardName,
                refBy = row.Cell(16).GetValue<string>(), // Column 16
                isValid = cardList.Any(x => x.Vchcname == cardName),
                error = cardList.Any(x => x.Vchcname == cardName) ? "" : "Invalid Card Name"
            });
            total += amt;
        }
        return new JsonResult(new { preview, totalAmount = total });
    }

    // BULK SAVE (Updated to map RefBy to DB)
    public async Task<IActionResult> OnPostUploadEditedAsync([FromBody] List<UploadDto> data)
    {
        var cards = await context.TblCardMaster.ToListAsync();
        foreach (var d in data)
        {
            var card = cards.FirstOrDefault(x => x.Vchcname == d.CardName);
            if (card == null) continue;

            context.TblCardRegistration.Add(new TblCardRegistration
            {
                Vchname = d.Name,
                Vchcontactno = d.Mobile,
                DtDob = DateTime.TryParse(d.Dob, out var dt) ? dt : null,
                Vchsex = d.Gender,
                Intage = int.TryParse(d.Age, out var ag) ? ag : 0,
                VchspouseName = d.Spouse,
                Vchemail = d.Email,
                VchAddress = d.Address,
                Vchcity = d.City,
                VchsState = d.State,
                Vchpincode = d.Pincode,
                VchUhidno = d.Uhid,
                VchHmsRcpt = d.Receipt,
                IntCharges = d.Amount,
                IntCardId = card.IntCard,
                VchCardType = card.Vchcname,
                VchCardRefBy = d.RefBy, // Mapping to your DB Column
                Dtcreated = DateTime.Now,
                Vchcreatedby = User.Identity?.Name ?? "Admin"
            });
        }
        int count = await context.SaveChangesAsync();
        return new JsonResult(new { success = count });
    }

    private async Task LoadData() =>
        CardTypeList = new SelectList(await context.TblCardMaster.Where(x => x.BitActive).ToListAsync(), "IntCard", "Vchcname");

    public async Task<JsonResult> OnGetCardCostAsync(int id) =>
        new JsonResult(new { cost = await context.TblCardMaster.Where(x => x.IntCard == id).Select(x => x.Intcostcard).FirstOrDefaultAsync() ?? 0 });

    public class UploadDto
    {
        public string Name { get; set; } = ""; public string Mobile { get; set; } = "";
        public string Dob { get; set; } = ""; public string Gender { get; set; } = "";
        public string Age { get; set; } = ""; public string Spouse { get; set; } = "";
        public string Email { get; set; } = ""; public string Address { get; set; } = "";
        public string City { get; set; } = ""; public string State { get; set; } = "";
        public string Pincode { get; set; } = ""; public string Uhid { get; set; } = "";
        public string Receipt { get; set; } = ""; public int Amount { get; set; }
        public string CardName { get; set; } = ""; public string RefBy { get; set; } = "";
    }

    public async Task AddToSyncQueue(string module, int recordId)
    {
        var servers = await context.TblServerRegistry.Where(x => x.BitIsActive==true).ToListAsync();
        foreach (var server in servers)
        {
            context.TblSyncQueue.Add(new TblSyncQueue
            {
                IntServerId = server.IntServerId,
                VchModule = module,
                IntRecordId = recordId,
                BitProcessed = false
            });
        }
        await context.SaveChangesAsync();
    }
}