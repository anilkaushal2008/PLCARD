using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PLCARD.Models;
using System.Diagnostics;
using Microsoft.Data.SqlClient;
using System.Net.Http.Json;

namespace PLCARD.Pages.Admin
{
    public class SyncCenterModel : PageModel
    {
        private readonly PLCARDContext _context;
        private readonly IHttpClientFactory _clientFactory;
        private readonly string _connectionString;

        public SyncCenterModel(PLCARDContext context, IHttpClientFactory clientFactory, IConfiguration configuration)
        {
            _context = context;
            _clientFactory = clientFactory;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public List<ServerStatusViewModel> ServerStatuses { get; set; } = new();
        public int OnlineCount => ServerStatuses.Count(s => s.IsOnline);

        public async Task OnGetAsync()
        {
            // 1. Fetch all active servers from ServerMaster
            var servers = await _context.ServerMaster.Where(s => s.BitIsActive == true).ToListAsync();

            // 2. Fetch HQ Totals once at the top (Grand totals for comparison)
            int totalHqCards = await _context.TblCardRegistration.CountAsync();
            int totalHqComps = await _context.TblCompanyRegistration.CountAsync();

            // 3. Setup HttpClient with a handler to allow hospital/local IPs
            var handler = new HttpClientHandler()
            {
                ServerCertificateCustomValidationCallback = (m, c, ch, e) => true
            };

            using var client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromSeconds(15);

            // 4. Run Telemetry Checks for each server
            var tasks = servers.Select(async server =>
            {
                // Retrieve dynamic URLs for Health (2), Card Count (3), and Corp Count (4)
                string healthUrl = await GetEndpointUrl(server.IntServerId, 2);
                string cardUrl = await GetEndpointUrl(server.IntServerId, 3);
                string compUrl = await GetEndpointUrl(server.IntServerId, 4);

                var status = new ServerStatusViewModel
                {
                    ServerName = server.VchServerName,
                    Location = server.VchLocation,
                    ApiUrl = healthUrl,
                    CentralCardCount = totalHqCards,
                    CentralCompanyCount = totalHqComps,
                    // Initialize hub counts as null (representing "Not Checked Yet")
                    HubCardCount = null,
                    HubCompanyCount = null
                };

                if (string.IsNullOrEmpty(healthUrl)) return status;

                var stopwatch = Stopwatch.StartNew();
                try
                {
                    // --- STEP 1: HEALTH CHECK (The "Pipe" Check) ---
                    var healthResponse = await client.GetAsync(healthUrl);
                    stopwatch.Stop();

                    status.IsOnline = healthResponse.IsSuccessStatusCode;
                    status.LatencyMs = stopwatch.ElapsedMilliseconds;

                    // --- STEP 2: DATA AUDIT (The "Water" Check) ---
                    if (status.IsOnline)
                    {
                        // Try to fetch Card Count - If this fails, HubCardCount stays null
                        try
                        {
                            var cRes = await client.GetFromJsonAsync<HubCountDTO>(cardUrl);
                            status.HubCardCount = cRes?.totalCards?.FirstOrDefault()?.totalCount ?? 0;
                        }
                        catch { status.HubCardCount = null; }

                        // Try to fetch Company Count - If this fails, HubCompanyCount stays null
                        try
                        {
                            var coRes = await client.GetFromJsonAsync<HubCountDTO>(compUrl);
                            status.HubCompanyCount = coRes?.totalCompanies?.FirstOrDefault()?.totalCount ?? 0;
                        }
                        catch { status.HubCompanyCount = null; }
                    }
                }
                catch (Exception ex)
                {
                    status.IsOnline = false;
                    status.ErrorMessage = ex.Message;
                }

                status.LastCheck = DateTime.Now;
                return status;
            });

            ServerStatuses = (await Task.WhenAll(tasks)).ToList();
        }

       
        public class ServerStatusViewModel
        {
            public string ServerName { get; set; } = "";
            public string Location { get; set; } = "";
            public string ApiUrl { get; set; } = "";
            public bool IsOnline { get; set; }
            public long LatencyMs { get; set; }
            public DateTime LastCheck { get; set; }
            public string ErrorMessage { get; set; } = "";
            public int CentralCardCount { get; set; }
            public int CentralCompanyCount { get; set; }

            // Changed to nullable to detect "API Fail" vs "Zero Records"
            public int? HubCardCount { get; set; }
            public int? HubCompanyCount { get; set; }
        }

        public class HubCountDTO
        {
            // Matches the nested JSON array structure from your Swagger
            public List<TotalCountDetail> totalCompanies { get; set; }
            public List<TotalCountDetail> totalCards { get; set; }
        }

        public class TotalCountDetail
        {
            public int totalCount { get; set; }
        }

        private async Task<string> GetEndpointUrl(int serverId, int serviceTypeId)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = new SqlCommand("SELECT VchEndpointUrl FROM TblHubServiceEndpoints WHERE IntServerId = @SId AND ServiceTypeId = @STId", conn);
            cmd.Parameters.AddWithValue("@SId", serverId);
            cmd.Parameters.AddWithValue("@STId", serviceTypeId);
            var result = await cmd.ExecuteScalarAsync();
            return result?.ToString() ?? string.Empty;
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
        public string ErrorMessage { get; set; } = "";
        public int CentralCardCount { get; set; }
        public int? HubCardCount { get; set; }
        public int CentralCompanyCount { get; set; }
        public int? HubCompanyCount { get; set; }
       
    }

    public class HubCountDTO
    {
        public List<TotalCountDetail> totalCompanies { get; set; }
        public List<TotalCountDetail> totalCards { get; set; }
    }

    public class TotalCountDetail { public int totalCount { get; set; } }
}