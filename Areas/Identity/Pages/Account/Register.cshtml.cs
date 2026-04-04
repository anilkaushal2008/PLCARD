using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

[Authorize(Roles = "Admin")] // CRITICAL: Only you can access this page now
public class RegisterModel : PageModel
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<RegisterModel> _logger;

    public RegisterModel(
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<RegisterModel> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; }

    public SelectList RoleList { get; set; } // For the UI Dropdown

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }

        [Required]
        public string Role { get; set; } // The selected role
    }

    public async Task OnGetAsync()
    {
        // Load roles from DB into the dropdown
        var roles = await _roleManager.Roles.Select(x => x.Name).ToListAsync();
        RoleList = new SelectList(roles);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (ModelState.IsValid)
        {
            var user = new IdentityUser { UserName = Input.Email, Email = Input.Email, EmailConfirmed = true };
            var result = await _userManager.CreateAsync(user, Input.Password);

            if (result.Succeeded)
            {
                // Assign the role you picked in the UI
                await _userManager.AddToRoleAsync(user, Input.Role);

                _logger.LogInformation("Admin created a new user with role.");
                return RedirectToPage("/Admin/ALLUser"); // Go back to your list
            }
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        // If something failed, reload the roles for the dropdown
        var rolesList = await _roleManager.Roles.Select(x => x.Name).ToListAsync();
        RoleList = new SelectList(rolesList);
        return Page();
    }
}