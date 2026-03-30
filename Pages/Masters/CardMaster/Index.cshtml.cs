using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PLCARD.Data;
using PLCARD.Models;

namespace PLCARD.Pages.Masters.CardMaster;

public class IndexModel(PLCARDContext context) : PageModel
{
    [BindProperty]
    public TblCardMaster CardModel { get; set; } = new();

    public IList<TblCardMaster> CardPlans { get; set; } = default!;

    public async Task OnGetAsync()
    {
        CardPlans = await context.TblCardMaster
            .OrderBy(m => m.Vchcname)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        // 1. Validate the Model (Respects 'Required' and 'Range' attributes)
        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            return Page();
        }

        if (CardModel.IntCard == 0)
        {
            // --- CREATE LOGIC ---
            CardModel.BitActive = true;
            context.TblCardMaster.Add(CardModel);
        }
        else
        {
            // --- EDIT LOGIC ---
            var planInDb = await context.TblCardMaster.FindAsync(CardModel.IntCard);

            if (planInDb != null)
            {
                // Update every field from the Modal
                planInDb.Vchcname = CardModel.Vchcname;
                planInDb.Intcostcard = CardModel.Intcostcard;
                planInDb.Flopd = CardModel.Flopd;
                planInDb.Flipd = CardModel.Flipd;
                planInDb.Fllab = CardModel.Fllab;
                planInDb.Flpharmacy = CardModel.Flpharmacy;
                planInDb.Fldiagno = CardModel.Fldiagno;
                planInDb.Flhomecare = CardModel.Flhomecare;
                planInDb.Flambulanceipd = CardModel.Flambulanceipd;

                context.TblCardMaster.Update(planInDb);
            }
        }

        await context.SaveChangesAsync();
        return RedirectToPage();
    }

    // Toggle Status Handler
    public async Task<IActionResult> OnPostToggleStatusAsync(int id)
    {
        var plan = await context.TblCardMaster.FindAsync(id);
        if (plan != null)
        {
            plan.BitActive = !plan.BitActive;
            await context.SaveChangesAsync();
        }
        return RedirectToPage();
    }
}
