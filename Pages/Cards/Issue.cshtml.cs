using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PLCARD.Data;
using PLCARD.Models;
using System.Text.Json.Serialization;

namespace PLCARD.Pages.Cards
{
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
            if (!string.IsNullOrEmpty(Registration.VchUhidno))
            {
                bool exists = await context.TblCardRegistration.AnyAsync(x => x.VchUhidno == Registration.VchUhidno && x.IntRegId != Registration.IntRegId);
                if (exists) { ModelState.AddModelError("Registration.VchUhidno", "UHID already registered."); await LoadData(); return Page(); }
            }
            var cardMaster = await context.TblCardMaster.FindAsync(Registration.IntCardId);
            if (cardMaster != null) Registration.VchCardType = cardMaster.Vchcname;
            if (Registration.IntRegId == 0) { Registration.Dtcreated = DateTime.Now; Registration.Vchcreatedby = User.Identity?.Name ?? "Admin"; context.TblCardRegistration.Add(Registration); }
            else { context.Attach(Registration).State = EntityState.Modified; }
            await context.SaveChangesAsync();
            await AddToSyncQueue("CARD", Registration.IntRegId);
            return RedirectToPage("./Index");
        }

        public async Task<JsonResult> OnPostPreviewExcelAsync(IFormFile ExcelFile)
        {
            if (ExcelFile == null) return new JsonResult(new { error = "Select file" });
            var preview = new List<object>();
            var cardList = await context.TblCardMaster.ToListAsync();
            var existingUhids = await context.TblCardRegistration.Where(x => !string.IsNullOrEmpty(x.VchUhidno)).Select(x => x.VchUhidno).ToListAsync();

            using var stream = new MemoryStream();
            await ExcelFile.CopyToAsync(stream);
            using var wb = new XLWorkbook(stream);
            var rows = wb.Worksheet(1).RangeUsed().RowsUsed().Skip(1);
            int total = 0;
            foreach (var row in rows)
            {
                var cardName = row.Cell(15).GetValue<string>()?.Trim() ?? "";
                var uhid = row.Cell(12).GetValue<string>()?.Trim() ?? "";
                var amt = row.Cell(14).GetValue<int>();

                bool isValidCard = cardList.Any(x => x.Vchcname.Trim().Equals(cardName, StringComparison.OrdinalIgnoreCase));
                bool isDuplicate = existingUhids.Contains(uhid);
                string errorMsg = !isValidCard ? "Invalid Card Name" : (isDuplicate ? "UHID Duplicate" : "");

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
                    uhid = uhid,
                    receipt = row.Cell(13).GetValue<string>(),
                    amount = amt,
                    cardName = cardName,
                    refBy = row.Cell(16).GetValue<string>(),
                    isValid = (isValidCard && !isDuplicate),
                    error = errorMsg
                });
                total += amt;
            }
            return new JsonResult(new { preview, totalAmount = total, cardList = cardList.Select(x => x.Vchcname).ToList() });
        }

        public async Task<IActionResult> OnPostUploadEditedAsync([FromBody] List<UploadDto> data)
        {
            if (data == null || data.Count == 0) return new JsonResult(new { error = "No data" });

            var cards = await context.TblCardMaster.ToListAsync();
            var newEntries = new List<TblCardRegistration>();

            foreach (var d in data)
            {
                var card = cards.FirstOrDefault(x => x.Vchcname.Trim().Equals(d.CardName?.Trim(), StringComparison.OrdinalIgnoreCase));
                if (card == null) continue;

                var reg = new TblCardRegistration
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
                    VchUhidno = d.Uhid?.Trim(),
                    VchHmsRcpt = d.Receipt,
                    IntCharges = d.Amount,
                    IntCardId = card.IntCard,
                    VchCardType = card.Vchcname,
                    VchCardRefBy = d.RefBy,
                    Dtcreated = DateTime.Now,
                    Vchcreatedby = User.Identity?.Name ?? "Admin",
                    FkBId = 1
                };
                context.TblCardRegistration.Add(reg);
                newEntries.Add(reg);
            }

            await context.SaveChangesAsync();           
            return new JsonResult(new { success = newEntries.Count });
        }

        private async Task LoadData() => CardTypeList = new SelectList(await context.TblCardMaster.Where(x => x.BitActive).ToListAsync(), "IntCard", "Vchcname");

        public async Task AddToSyncQueue(string module, int recordId)
        {
            var servers = await context.ServerMaster.Where(x => x.BitIsActive == true).ToListAsync();
            foreach (var server in servers)
            {
                context.TblSyncQueue.Add(new TblSyncQueue
                {
                    IntServerId = server.IntServerId,
                    VchModule = module,
                    IntRecordId = recordId,
                    BitProcessed = false,
                    DtAdded = DateTime.Now
                });
            }
            await context.SaveChangesAsync();
        }

        public async Task<JsonResult> OnGetCardCostAsync(int id) => new JsonResult(new { cost = await context.TblCardMaster.Where(x => x.IntCard == id).Select(x => x.Intcostcard).FirstOrDefaultAsync() ?? 0 });

        public IActionResult OnGetDownloadTemplate()
        {
            using var wb = new XLWorkbook(); var ws = wb.Worksheets.Add("Cards");
            string[] h = { "Name", "Mobile", "DOB", "Gender", "Age", "Spouse", "Email", "Address", "City", "State", "Pin", "UHID", "Receipt", "Amount", "CardName", "RefBy" };
            for (int i = 0; i < h.Length; i++) ws.Cell(1, i + 1).Value = h[i];
            ws.RangeUsed().Style.Font.Bold = true; ws.Columns().AdjustToContents();
            using var ms = new MemoryStream(); wb.SaveAs(ms); return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "CardTemplate.xlsx");
        }
    }

    public class UploadDto
    {
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("mobile")] public string Mobile { get; set; } = "";
        [JsonPropertyName("dob")] public string Dob { get; set; } = "";
        [JsonPropertyName("gender")] public string Gender { get; set; } = "";
        [JsonPropertyName("age")] public string Age { get; set; } = "";
        [JsonPropertyName("spouse")] public string Spouse { get; set; } = "";
        [JsonPropertyName("email")] public string Email { get; set; } = "";
        [JsonPropertyName("address")] public string Address { get; set; } = "";
        [JsonPropertyName("city")] public string City { get; set; } = "";
        [JsonPropertyName("state")] public string State { get; set; } = "";
        [JsonPropertyName("pincode")] public string Pincode { get; set; } = "";
        [JsonPropertyName("uhid")] public string Uhid { get; set; } = "";
        [JsonPropertyName("receipt")] public string Receipt { get; set; } = "";
        [JsonPropertyName("amount")] public int Amount { get; set; }
        [JsonPropertyName("cardName")] public string CardName { get; set; } = "";
        [JsonPropertyName("refBy")] public string RefBy { get; set; } = "";
    }
}