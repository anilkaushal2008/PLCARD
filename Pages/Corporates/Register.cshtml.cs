using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PLCARD.Data;
using PLCARD.Models;

namespace PLCARD.Pages.Corporates
{
    public class RegisterModel : PageModel
    {
        private readonly PLCARDContext _context;
        private readonly IWebHostEnvironment _environment;

        // TRADITIONAL CONSTRUCTOR (Fixes Hot Reload Error)
        public RegisterModel(PLCARDContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [BindProperty]
        public TblCompanyRegistration Company { get; set; } = new();

        [BindProperty]
        public IFormFile? AgreementUpload { get; set; }

        public SelectList PlanList { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            await LoadPlans();

            if (id.HasValue && id > 0)
            {
                Company = await _context.TblCompanyRegistration.FindAsync(id.Value);
                if (Company == null) return NotFound();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadPlans();
                return Page();
            }

            if (AgreementUpload != null)
            {
                var ext = Path.GetExtension(AgreementUpload.FileName).ToLower();
                if (ext != ".pdf")
                {
                    ModelState.AddModelError("AgreementUpload", "Only PDF files are allowed.");
                    await LoadPlans();
                    return Page();
                }

                string folder = Path.Combine(_environment.WebRootPath, "uploads", "agreements");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                string fileName = $"{Guid.NewGuid()}{ext}";
                string filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await AgreementUpload.CopyToAsync(stream);
                }

                Company.VchAgreementPath = "/uploads/agreements/" + fileName;
            }

            string currentUser = User.Identity?.Name ?? "Admin";
            string currentHost = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Local";

            if (Company.IntCompanyId == 0)
            {
                Company.BitActive = true;
                Company.DtRegistration = DateTime.Now;
                Company.VchCreatedBy = currentUser;
                Company.VchCreatedHost = currentHost;
                _context.TblCompanyRegistration.Add(Company);
            }
            else
            {
                var dbEntry = await _context.TblCompanyRegistration.FindAsync(Company.IntCompanyId);
                if (dbEntry == null) return NotFound();

                dbEntry.VchCompanyName = Company.VchCompanyName;
                dbEntry.IntPlanId = Company.IntPlanId;
                dbEntry.VchAgreementNo = Company.VchAgreementNo;
                dbEntry.DtAgreementStart = Company.DtAgreementStart;
                dbEntry.DtAgreementEnd = Company.DtAgreementEnd;
                dbEntry.VchGstNo = Company.VchGstNo;
                dbEntry.VchPanNo = Company.VchPanNo;
                dbEntry.BitIsCredit = Company.BitIsCredit;
                dbEntry.VchContactPerson = Company.VchContactPerson;
                dbEntry.VchDesignation = Company.VchDesignation;
                dbEntry.VchContactNo = Company.VchContactNo;
                dbEntry.VchEmail = Company.VchEmail;
                dbEntry.VchFullAddress = Company.VchFullAddress;
                dbEntry.VchCity = Company.VchCity;
                dbEntry.VchPincode = Company.VchPincode;

                if (AgreementUpload != null)
                    dbEntry.VchAgreementPath = Company.VchAgreementPath;

                dbEntry.DtUpdated = DateTime.Now;
                dbEntry.VchUpdatedBy = currentUser;
                dbEntry.VchUpdatedHost = currentHost;

                _context.TblCompanyRegistration.Update(dbEntry);
            }

            await _context.SaveChangesAsync();
            return RedirectToPage("./Index");
        }

        private async Task LoadPlans()
        {
            var plans = await _context.TblCorporatePlanMaster
                .Where(p => p.BitActive == true)
                .ToListAsync();

            PlanList = new SelectList(plans, "IntPlanId", "VchPlanName");
        }

    }
}