using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace PLCARD.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class RoleMasterModel : PageModel
    {
        private readonly RoleManager<IdentityRole> _roleManager;

        public RoleMasterModel(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }

        public List<IdentityRole> Roles { get; set; } = new();

        [BindProperty]
        public string NewRoleName { get; set; } = string.Empty;

        public async Task OnGetAsync()
        {
            Roles = await _roleManager.Roles.ToListAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!string.IsNullOrWhiteSpace(NewRoleName))
            {
                await _roleManager.CreateAsync(new IdentityRole(NewRoleName.Trim()));
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role != null && role.Name != "Admin") // Prevent deleting the Admin role
            {
                await _roleManager.DeleteAsync(role);
            }
            return RedirectToPage();
        }
    }
}