using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore; // CRITICAL: Added this for ToListAsync()
using PLCARD.Models;
using PLCARD.Services;

namespace PLCARD.Pages.Admin
{
    public class EmailAuditModel : PageModel
    {
        private readonly PLCARDContext _context;
        private readonly EmailNotificationService _emailService;

        public EmailAuditModel(PLCARDContext context, EmailNotificationService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public IList<EmailLogs> EmailLogsList { get; set; } = new List<EmailLogs>();

        public async Task OnGetAsync()
        {
            // Fetching logs with proper null handling
            EmailLogsList = await _context.EmailLogs
                .OrderByDescending(l => l.SentAt)
                .Take(500)
                .ToListAsync() ?? new List<EmailLogs>();
        }

        public async Task<IActionResult> OnPostResendAsync(int logId, string companyName)
        {
            // 1. Trigger the email service
            // We keep the service call general as per your existing logic
            await _emailService.SendCorporateNotificationAsync(companyName);

            // 2. Find the specific failed log in the database
            var existingLog = await _context.EmailLogs.FindAsync(logId);

            if (existingLog != null)
            {
                // 3. Update the existing record to Success
                existingLog.SentStatus = "Success";
                existingLog.SentAt = DateTime.Now; // Update to latest time
                existingLog.ErrorMessage = null;   // Clear the old "Failure sending mail" error

                await _context.SaveChangesAsync();
                TempData["Message"] = $"Email for {companyName} has been successfully resent and updated.";
            }

            return RedirectToPage();
        }
    }
}