using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PLCARD.Models;

namespace PLCARD.Pages.Admin
{
    public class EmailMasterModel : PageModel
    {
        private readonly PLCARDContext _context;

        public EmailMasterModel(PLCARDContext context)
        {
            _context = context;
        }

        public IList<EmailMasters> EmailRecipients { get; set; } = default!;

        [BindProperty]
        public EmailMasters Recipient { get; set; } = new();

        public async Task OnGetAsync()
        {
            // Initialize list to prevent null reference errors in the View
            EmailRecipients = await _context.EmailMasters.OrderBy(x => x.RecipientName).ToListAsync()
                              ?? new List<EmailMasters>();
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            // Ensure nullable bool has a value before saving
            Recipient.IsActive = Recipient.IsActive ?? false;

            if (Recipient.Id == 0)
            {
                _context.EmailMasters.Add(Recipient);
                TempData["Message"] = "Recipient added successfully.";
            }
            else
            {
                _context.Attach(Recipient).State = EntityState.Modified;
                TempData["Message"] = "Recipient updated successfully.";
            }

            await _context.SaveChangesAsync();
            return RedirectToPage();
        }

        public async Task<JsonResult> OnGetFetchRecipient(int id)
        {
            var data = await _context.EmailMasters.FindAsync(id);
            return new JsonResult(data);
        }
    }
}