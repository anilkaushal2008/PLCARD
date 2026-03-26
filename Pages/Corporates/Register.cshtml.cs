using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using PLCARD.Data;
using PLCARD.Models;

namespace PLCARD.Pages.Corporates;

public class RegisterModel(ApplicationDbContext context, IWebHostEnvironment environment) : PageModel
{
    [BindProperty]
    public TblCompanyRegistration Company { get; set; } = default!;

    [BindProperty]
    public IFormFile? UploadedAgreement { get; set; }

    public SelectList PlanList { get; set; } = default!;

    public void OnGet()
    {
        // Load Platinum and Diamond plans for the dropdown
        PlanList = new SelectList(context.TblCorporatePlanMasters, "IntPlanId", "VchPlanName");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (UploadedAgreement != null)
        {
            // Create a unique filename to avoid overwriting
            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(UploadedAgreement.FileName);
            string filePath = Path.Combine(environment.WebRootPath, "uploads", fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await UploadedAgreement.CopyToAsync(stream);
            }

            // Save the path in the database
            Company.VchAgreementPath = "/uploads/" + fileName;
        }

        Company.DtRegistration = DateTime.Now;
        Company.BitActive = true;
        Company.VchCreatedBy = User.Identity?.Name ?? "Admin";

        context.TblCompanyRegistrations.Add(Company);
        await context.SaveChangesAsync();

        return RedirectToPage("../Index");
    }
}