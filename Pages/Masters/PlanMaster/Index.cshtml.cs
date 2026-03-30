using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PLCARD.Data;
using PLCARD.Models;

namespace PLCARD.Pages.Masters.PlanMaster
{
    public class IndexModel(PLCARDContext context) : PageModel
    {
        [BindProperty]
        public TblCorporatePlanMaster PlanModel { get; set; } = new();

        public IList<TblCorporatePlanMaster> CorporatePlans { get; set; } = default!;

        public async Task OnGetAsync()
        {
            // Filters out deleted and orders by name
            CorporatePlans = await context.TblCorporatePlanMaster
                .Where(m => m.BitDeleted != true)
                .OrderBy(m => m.VchPlanName)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                return Page();
            }

            if (PlanModel.IntPlanId == 0)
            {
                // NEW RECORD
                PlanModel.BitActive = true;
                PlanModel.BitDeleted = false;
                PlanModel.DtCreated = DateTime.Now;
                PlanModel.VchCreatedBy = User.Identity?.Name ?? "Admin";
                context.TblCorporatePlanMaster.Add(PlanModel);
            }
            else
            {
                // EDIT RECORD
                var dbEntry = await context.TblCorporatePlanMaster.FindAsync(PlanModel.IntPlanId);
                if (dbEntry != null)
                {
                    dbEntry.VchPlanName = PlanModel.VchPlanName;
                    dbEntry.DcOpdConsult = PlanModel.DcOpdConsult;
                    dbEntry.DcOpdProcedure = PlanModel.DcOpdProcedure;
                    dbEntry.DcRadiology = PlanModel.DcRadiology;
                    dbEntry.DcLab = PlanModel.DcLab;
                    dbEntry.DcPharmacy = PlanModel.DcPharmacy;
                    dbEntry.DcHomecare = PlanModel.DcHomecare;
                    dbEntry.DcIpd = PlanModel.DcIpd;
                    dbEntry.DcAmbulance = PlanModel.DcAmbulance;

                    dbEntry.DtUpdated = DateTime.Now;
                    dbEntry.VchUpdatedBy = User.Identity?.Name ?? "Admin";
                    context.TblCorporatePlanMaster.Update(dbEntry);
                }
            }

            await context.SaveChangesAsync();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostToggleStatusAsync(int id)
        {
            var plan = await context.TblCorporatePlanMaster.FindAsync(id);
            if (plan != null)
            {
                plan.BitActive = !(plan.BitActive ?? false);
                await context.SaveChangesAsync();
            }
            return RedirectToPage();
        }
    }
}