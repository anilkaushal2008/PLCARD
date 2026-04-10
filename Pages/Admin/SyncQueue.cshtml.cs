using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
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

        public List<TblSyncQueue> SyncQueue { get; set; } = new();

        public async Task OnGetAsync()
        {
            // FIX 1: Change 'Include(q => q.IntServerId)' to 'Include(q => q.IntServer)'
            // This pulls the actual ServerMaster object into the queue items.
            SyncQueue = await _context.TblSyncQueue
                .Include(q => q.IntServer)
                .OrderByDescending(q => q.DtAdded)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostRetryFailedAsync()
        {
            var failedItems = await _context.TblSyncQueue
                .Where(q => q.IntRetryCount >= 5 && q.BitProcessed != true)
                .ToListAsync();

            foreach (var item in failedItems)
            {
                item.IntRetryCount = 0;
                item.VchErrorLog = "Retrying..."; // Clear the error log for the next attempt
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