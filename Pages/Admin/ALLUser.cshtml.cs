using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace PLCARD.Pages.Admin
{
    [Authorize(Roles = "Admin")] // Only the Admin role can access this page
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

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                UsersList.Add(new UserDisplayViewModel
                {
                    Id = user.Id,
                    Email = user.Email ?? "N/A",
                    Username = user.UserName ?? "N/A",
                    Roles = roles.ToList()
                });
            }
        }
    }

    // Simple class to hold the data for the table
    public class UserDisplayViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
    }
}