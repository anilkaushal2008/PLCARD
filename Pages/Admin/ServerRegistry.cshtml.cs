using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PLCARD.Models;

namespace PLCARD.Pages.Admin
{
    public class ServerRegistryModel : PageModel
    {
        private readonly PLCARDContext _context;

        public ServerRegistryModel(PLCARDContext context)
        {
            _context = context;
        }

        // List to display in the main table
        public IList<TblServerRegistry> ServerRegistries { get; set; } = default!;

        // Object bound to the Modal Form
        [BindProperty]
        public TblServerRegistry ServerEntry { get; set; } = new();

        public async Task OnGetAsync()
        {
            // Load all hubs for the dashboard list
            ServerRegistries = await _context.TblServerRegistry.ToListAsync();
        }

        // AJAX Handler: Fetches data when the "Edit" button is clicked
        public async Task<JsonResult> OnGetFetchServer(int id)
        {
            var data = await _context.TblServerRegistry
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.IntServerId == id);

            return new JsonResult(data);
        }

        // Save Handler: Handles both creating new hubs and updating existing ones
        public async Task<IActionResult> OnPostSaveAsync()
        {
            // 1. Manually handle the checkbox to avoid the "true,false" string error
            var checkValue = Request.Form["ServerEntry.BitIsActive"].ToString();
            ServerEntry.BitIsActive = checkValue.Contains("true");

            // 2. Clear validation for the checkbox since we handled it manually
            ModelState.Remove("ServerEntry.BitIsActive");

            if (!ModelState.IsValid)
            {
                ServerRegistries = await _context.TblServerRegistry.ToListAsync();
                return Page();
            }

            if (ServerEntry.IntServerId == 0)
            {
                // ADD NEW
                _context.TblServerRegistry.Add(ServerEntry);
                TempData["Message"] = $"Hub '{ServerEntry.VchServerName}' registered successfully!";
            }
            else
            {
                // EDIT EXISTING
                _context.Attach(ServerEntry).State = EntityState.Modified;
                TempData["Message"] = $"Hub '{ServerEntry.VchServerName}' updated successfully!";
            }

            await _context.SaveChangesAsync();
            return RedirectToPage();
        }
    }
}