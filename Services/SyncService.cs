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
            var servers = await _context.TblServerRegistry
                .Where(x => x.BitIsActive==true)
                .ToListAsync();

            foreach (var server in servers)
            {
                _context.TblSyncQueue.Add(new TblSyncQueue
                {
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