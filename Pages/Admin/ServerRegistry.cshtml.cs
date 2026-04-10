using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using PLCARD.Models;

namespace PLCARD.Pages.Admin
{
    public class ServerRegistryModel : PageModel
    {
        private readonly PLCARDContext _context;
        private readonly string _connectionString;

        public ServerRegistryModel(PLCARDContext context, IConfiguration configuration)
        {
            _context = context;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public IList<ServerMaster> ServerRegistries { get; set; } = default!;
        public List<ServiceType> ActiveServices { get; set; } = new();

        [BindProperty]
        public ServerMaster ServerEntry { get; set; } = new();

        public async Task OnGetAsync()
        {
            // Load the main list
            ServerRegistries = await _context.ServerMaster.ToListAsync();

            // Fetch the Master Service Types (for the Dynamic UI)
            ActiveServices = await GetActiveServiceTypes();
        }

        private async Task<List<ServiceType>> GetActiveServiceTypes()
        {
            var list = new List<ServiceType>();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand("SELECT ServiceTypeId, ServiceName, DefaultMethod FROM ServiceTypeMaster WHERE IsActive = 1", conn);
                using var rdr = await cmd.ExecuteReaderAsync();
                while (await rdr.ReadAsync())
                {
                    list.Add(new ServiceType
                    {
                        ServiceTypeId = (int)rdr["ServiceTypeId"],
                        ServiceName = rdr["ServiceName"].ToString(),
                        DefaultMethod = rdr["DefaultMethod"].ToString()
                    });
                }
            }
            return list;
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            // 1. Save Hub Identity
            var checkValue = Request.Form["ServerEntry.BitIsActive"].ToString();
            ServerEntry.BitIsActive = checkValue.Contains("true");
            ModelState.Remove("ServerEntry.BitIsActive");

            if (ServerEntry.IntServerId == 0)
            {
                ServerEntry.DtCreated = DateTime.Now;
                ServerEntry.VchCreatedBy = User.Identity?.Name ?? "Admin";
                _context.ServerMaster.Add(ServerEntry);
            }
            else
            {
                _context.Attach(ServerEntry).State = EntityState.Modified;
            }

            await _context.SaveChangesAsync();

            // 2. Save Dynamic URLs to the Link Table
            await SaveHubEndpoints(ServerEntry.IntServerId, Request.Form);

            TempData["Message"] = "Hub Configuration Saved Successfully!";
            return RedirectToPage();
        }

        private async Task SaveHubEndpoints(int hubId, IFormCollection form)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                // Clear old endpoints first
                var deleteCmd = new SqlCommand("DELETE FROM TblHubServiceEndpoints WHERE IntServerId = @HubId", conn);
                deleteCmd.Parameters.AddWithValue("@HubId", hubId);
                await deleteCmd.ExecuteNonQueryAsync();

                // Insert new ones from the form
                foreach (var key in form.Keys.Where(k => k.StartsWith("ServiceURL_")))
                {
                    string url = form[key];
                    if (!string.IsNullOrEmpty(url))
                    {
                        int serviceId = int.Parse(key.Replace("ServiceURL_", ""));
                        var ins = new SqlCommand("INSERT INTO TblHubServiceEndpoints (IntServerId, ServiceTypeId, VchEndpointUrl) VALUES (@HId, @SId, @Url)", conn);
                        ins.Parameters.AddWithValue("@HId", hubId);
                        ins.Parameters.AddWithValue("@SId", serviceId);
                        ins.Parameters.AddWithValue("@Url", url);
                        await ins.ExecuteNonQueryAsync();
                    }
                }
            }
        }

        // AJAX: Fetch Server Data
        public async Task<JsonResult> OnGetFetchServer(int id)
        {
            var data = await _context.ServerMaster.AsNoTracking().FirstOrDefaultAsync(m => m.IntServerId == id);
            return new JsonResult(data);
        }

        // AJAX: Fetch Saved Endpoints
        public async Task<JsonResult> OnGetFetchEndpoints(int hubId)
        {
            var endpoints = new List<object>();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand("SELECT ServiceTypeId, VchEndpointUrl FROM TblHubServiceEndpoints WHERE IntServerId = @HubId", conn);
                cmd.Parameters.AddWithValue("@HubId", hubId);
                using var rdr = await cmd.ExecuteReaderAsync();
                while (await rdr.ReadAsync())
                {
                    endpoints.Add(new { serviceTypeId = (int)rdr["ServiceTypeId"], url = rdr["VchEndpointUrl"].ToString() });
                }
            }
            return new JsonResult(endpoints);
        }
    }
}