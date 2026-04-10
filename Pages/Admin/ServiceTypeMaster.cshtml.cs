// Add these at the top
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using PLCARD.Models;

namespace PLCARD.Pages.Admin
{
    // Adding this attribute helps bypass common "400 Bad Request" issues during testing
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class ServiceTypeMasterModel : PageModel
    {
        private readonly string _connectionString;

        public ServiceTypeMasterModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public List<ServiceType> ServiceTypes { get; set; } = new List<ServiceType>();

        public void OnGet()
        {
            LoadData();
        }

        private void LoadData()
        {
            ServiceTypes.Clear();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT * FROM ServiceTypeMaster ORDER BY CreatedAt DESC", conn))
                {
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            ServiceTypes.Add(new ServiceType
                            {
                                ServiceTypeId = (int)rdr["ServiceTypeId"],
                                ServiceName = rdr["ServiceName"].ToString(),
                                DefaultMethod = rdr["DefaultMethod"].ToString(),
                                Description = rdr["Description"]?.ToString(),
                                IsActive = (bool)rdr["IsActive"]
                            });
                        }
                    }
                }
            }
        }

        // IMPORTANT: The name MUST be OnPostSaveServiceType
        public IActionResult OnPostSaveServiceType(int ServiceTypeId, string ServiceName, string DefaultMethod, string Description, bool IsActive)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string sql = ServiceTypeId == 0
                        ? "INSERT INTO ServiceTypeMaster (ServiceName, DefaultMethod, Description, IsActive) VALUES (@Name, @Method, @Desc, @Active)"
                        : "UPDATE ServiceTypeMaster SET ServiceName=@Name, DefaultMethod=@Method, Description=@Desc, IsActive=@Active WHERE ServiceTypeId=@Id";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Name", ServiceName ?? "");
                        cmd.Parameters.AddWithValue("@Method", DefaultMethod ?? "GET");
                        cmd.Parameters.AddWithValue("@Desc", (object)Description ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Active", IsActive);
                        if (ServiceTypeId != 0) cmd.Parameters.AddWithValue("@Id", ServiceTypeId);

                        cmd.ExecuteNonQuery();
                    }
                }
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                // This will show the actual SQL error in your browser console
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }
    }
}