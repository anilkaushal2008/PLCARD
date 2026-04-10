using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace PLCARD.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class EditUserModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public EditUserModel(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [BindProperty]
        public string UserId { get; set; } = string.Empty;
        [BindProperty]
        public string Username { get; set; } = string.Empty;
        [BindProperty]
        public string? NewPassword { get; set; }

        [BindProperty]
        public List<string> SelectedRoles { get; set; } = new();
        public List<string> AllRoles { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            UserId = user.Id;
            Username = user.UserName ?? "";

            // Get all roles available in the system
            AllRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();

            // Get roles currently assigned to this user
            var userRoles = await _userManager.GetRolesAsync(user);
            SelectedRoles = userRoles.ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.FindByIdAsync(UserId);
            if (user == null) return NotFound();

            // 1. Update Password if provided
            if (!string.IsNullOrWhiteSpace(NewPassword))
            {
                await _userManager.RemovePasswordAsync(user);
                await _userManager.AddPasswordAsync(user, NewPassword);
            }

            // 2. Update Roles
            var currentRoles = await _userManager.GetRolesAsync(user);

            // Remove roles that are no longer selected
            var rolesToRemove = currentRoles.Except(SelectedRoles);
            await _userManager.RemoveFromRolesAsync(user, rolesToRemove);

            // Add new roles that were selected
            var rolesToAdd = SelectedRoles.Except(currentRoles);
            await _userManager.AddToRolesAsync(user, rolesToAdd);

            TempData["Message"] = $"Account for {user.UserName} updated successfully.";
            return RedirectToPage("/Admin/ALLUser");
        }
    }
}