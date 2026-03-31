using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PLCARD.Data;
using PLCARD.Models;

namespace PLCARD.Pages.Admin
{
    public class SyncQueueModel : PageModel
    {
        private readonly PLCARDContext _context;

        public SyncQueueModel(PLCARDContext context)
        {
            _context = context;
        }

        // --- THE HTML NEEDS THESE EXACT NAMES ---
        public List<TblSyncQueue> SyncQueue { get; set; } = new();
        public int PendingCount { get; set; }
        public int FailedCount { get; set; }

        public async Task OnGetAsync()
        {
            SyncQueue = await _context.TblSyncQueue
                .Include(q => q.IntServer) // Requires the Navigation Property we added
                .OrderByDescending(q => q.DtAdded)
                .Take(100)
                .ToListAsync();

            PendingCount = await _context.TblSyncQueue.CountAsync(q => q.BitProcessed != true);
            FailedCount = await _context.TblSyncQueue.CountAsync(q => q.IntRetryCount >= 5 && q.BitProcessed != true);
        }

        public async Task<IActionResult> OnPostRetryFailedAsync()
        {
            var failedItems = await _context.TblSyncQueue
                .Where(q => q.IntRetryCount >= 5 && q.BitProcessed != true)
                .ToListAsync();

            foreach (var item in failedItems)
            {
                item.IntRetryCount = 0;
            }

            await _context.SaveChangesAsync();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostClearProcessedAsync()
        {
            var processed = await _context.TblSyncQueue.Where(q => q.BitProcessed == true).ToListAsync();
            _context.TblSyncQueue.RemoveRange(processed);
            await _context.SaveChangesAsync();
            return RedirectToPage();
        }
    }
}