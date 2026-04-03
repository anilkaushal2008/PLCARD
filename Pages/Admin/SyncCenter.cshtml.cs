using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PLCARD.Models;
using System.Diagnostics;

namespace PLCARD.Pages.Admin // <--- THIS MUST MATCH YOUR FOLDER PATH
{
    public class SyncCenterModel : PageModel
    {
        private readonly PLCARDContext _context;
        private readonly IHttpClientFactory _clientFactory;

        public SyncCenterModel(PLCARDContext context, IHttpClientFactory clientFactory)
        {
            _context = context;
            _clientFactory = clientFactory;
        }

        public List<ServerStatusViewModel> ServerStatuses { get; set; } = new();
        public int OnlineCount => ServerStatuses.Count(s => s.IsOnline);

        public async Task OnGetAsync()
        {
            var servers = await _context.TblServerRegistry.Where(s => s.BitIsActive == true).ToListAsync();
            var client = _clientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);

            var tasks = servers.Select(async server =>
            {
                var status = new ServerStatusViewModel
                {
                    ServerName = server.VchServerName,
                    Location = server.VchLocation,
                    ApiUrl = server.VchApihealth
                };

                var stopwatch = Stopwatch.StartNew();
                try
                {
                    // Ping the base URL to check if the server is alive
                    var response = await client.GetAsync(server.VchApihealth);
                    stopwatch.Stop();

                    status.IsOnline = response.IsSuccessStatusCode;
                    status.LatencyMs = stopwatch.ElapsedMilliseconds;
                    status.LastCheck = DateTime.Now;
                }
                catch
                {
                    status.IsOnline = false;
                    status.LatencyMs = 0;
                    status.LastCheck = DateTime.Now;
                }
                return status;
            });

            ServerStatuses = (await Task.WhenAll(tasks)).ToList();
        }
    }

    public class ServerStatusViewModel
    {
        public string ServerName { get; set; } = "";
        public string Location { get; set; } = "";
        public string ApiUrl { get; set; } = "";
        public bool IsOnline { get; set; }
        public long LatencyMs { get; set; }
        public DateTime LastCheck { get; set; }
    }
}