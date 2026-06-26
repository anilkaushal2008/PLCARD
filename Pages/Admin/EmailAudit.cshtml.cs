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
        public async Task<IActionResult> OnPostResendAsync(int logId)
        {
            // 1. Call the targeted Resend method instead of the bulk service
            bool isSuccess = await _emailService.ResendCorporateNotificationAsync(logId);

            // 2. Set user feedback based on the result
            if (isSuccess)
            {
                TempData["Message"] = "Email re-sent successfully for the selected recipient.";
            }
            else
            {
                TempData["Message"] = "Failed to resend the email. Please check the Audit Trace.";
            }

            return RedirectToPage();
        }
    }
}