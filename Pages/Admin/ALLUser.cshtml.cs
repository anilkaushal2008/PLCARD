using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace PLCARD.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class ALLUserModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;

        public ALLUserModel(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        public List<UserDisplayViewModel> UsersList { get; set; } = new();

        public async Task OnGetAsync()
        {
            var users = await _userManager.Users.ToListAsync();
            UsersList.Clear();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                // In ASP.NET Identity, a user is "Deactivated" if they are currently Locked Out
                var isLocked = await _userManager.IsLockedOutAsync(user);

                UsersList.Add(new UserDisplayViewModel
                {
                    Id = user.Id,
                    Email = user.Email ?? "N/A",
                    Username = user.UserName ?? "N/A",
                    Roles = roles.ToList(),
                    IsActive = !isLocked
                });
            }
        }

        public async Task<IActionResult> OnPostToggleStatusAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Guard: Prevent admin from deactivating themselves
            if (user.UserName == User.Identity?.Name)
            {
                TempData["Error"] = "Security Guard: You cannot deactivate your own administrative account.";
                return RedirectToPage();
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                // Activate the user
                await _userManager.SetLockoutEndDateAsync(user, null);
                TempData["Message"] = $"Staff member '{user.UserName}' is now Active.";
            }
            else
            {
                // Deactivate the user (Lockout for 100 years)
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                TempData["Message"] = $"Staff member '{user.UserName}' has been Deactivated.";
            }

            return RedirectToPage();
        }
    }

    public class UserDisplayViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
        public bool IsActive { get; set; }
    }
}