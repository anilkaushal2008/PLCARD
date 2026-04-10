using Microsoft.EntityFrameworkCore;
using PLCARD.Data;
using PLCARD.Models;

namespace PLCARD.Services
{
    public class SyncService
    {
        private readonly PLCARDContext _context;

        public SyncService(PLCARDContext context)
        {
            _context = context;
        }

        public async Task AddToSyncQueue(string module, int recordId)
        {
            // NEW: Reference the new 'ServerMaster' table instead of the deleted registry
            var servers = await _context.ServerMaster
                .Where(x => x.BitIsActive == true)
                .ToListAsync();

            if (!servers.Any()) return;

            foreach (var server in servers)
            {
                _context.TblSyncQueue.Add(new TblSyncQueue
                {
                    // IntServerId matches the Foreign Key to ServerMaster
                    IntServerId = server.IntServerId,
                    VchModule = module,
                    IntRecordId = recordId,
                    BitProcessed = false,
                    DtAdded = DateTime.Now,
                    IntRetryCount = 0
                });
            }

            await _context.SaveChangesAsync();
        }
    }
}