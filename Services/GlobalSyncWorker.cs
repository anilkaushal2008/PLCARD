using Microsoft.EntityFrameworkCore;
using PLCARD.Data;
using PLCARD.Models;
using System.Net.Http.Json;
using PLCARD.Models.DTOs;

namespace PLCARD.Services;

public class GlobalSyncWorker(IServiceProvider serviceProvider, IHttpClientFactory httpClientFactory, ILogger<GlobalSyncWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Global Sync Worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessSyncQueue(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred in Global Sync Worker.");
            }

            // Wait for 15 minutes before the next run (adjustable via TblGlobalSettings)
            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }
    }

    private async Task ProcessSyncQueue(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PLCARDContext>();
        var http = httpClientFactory.CreateClient();

        // 1. Get all pending items from the queue (Handling nullable BitProcessed)
        var pendingItems = await context.TblSyncQueue
            .Where(q => q.BitProcessed != true && q.IntRetryCount < 5)
            .OrderBy(q => q.VchModule == "MASTER" ? 0 : 1)
            .ToListAsync(ct);

        if (!pendingItems.Any()) return;

        var groupedByServer = pendingItems.GroupBy(q => q.IntServerId);

        foreach (var serverGroup in groupedByServer)
        {
            var server = await context.TblServerRegistry.FindAsync(serverGroup.Key);

            // Handling nullable BitIsActive
            if (server == null || server.BitIsActive != true) continue;

            foreach (var item in serverGroup)
            {
                bool success = await PushRecordToRemote(server, item, context, http);

                if (success)
                {
                    item.BitProcessed = true;
                    UpdateRegistryTimestamp(server, item.VchModule);
                }
                else
                {
                    item.IntRetryCount++;
                }
            }
            await context.SaveChangesAsync(ct);
        }
    }
    private async Task<bool> PushRecordToRemote(TblServerRegistry server, TblSyncQueue queueItem, PLCARDContext db, HttpClient http)
    {
        try
        {
            // 1. Prepare the specific DTO (the "envelope") for the API
            object? payload = null;

            if (queueItem.VchModule == "CORP")
            {
                var company = await db.TblCompanyRegistration.FindAsync(queueItem.IntRecordId);
                if (company == null) return true; // Record was deleted, mark as processed

                // Map database entity to the light-weight DTO
                payload = new ComapnySyncDTO
                {
                    IntCompanyId = company.IntCompanyId,
                    VchCompanyName = company.VchCompanyName,
                    IntPlanId = company.IntPlanId,
                    VchContactPerson = company.VchContactPerson,
                    VchContactNo = company.VchContactNo,
                    VchEmail = company.VchEmail,
                    VchGstNo = company.VchGstNo,
                    VchPanNo = company.VchPanNo,
                    VchPincode = company.VchPincode
                };
            }
            else if (queueItem.VchModule == "CARD")
            {
                // We can add the CardSyncDTO mapping here next!
                //var card = await db.TblCardRegistration.FindAsync(queueItem.IntRecordId);
                //if (card == null) return true;
                //payload = card; // For now, or map to CardSyncDTO
            }

            if (payload == null) return true;

            // 2. Set up headers (using the key from your Registry)
            http.DefaultRequestHeaders.Clear();
            if (!string.IsNullOrEmpty(server.VchApiKey))
            {
                http.DefaultRequestHeaders.Add("X-Sync-Key", server.VchApiKey);
            }

            // 3. Send the Data
            // Note: We use server.VchApiUrl directly because it already contains the full path
            var response = await http.PostAsJsonAsync(server.VchApiUrl, payload);

            if (!response.IsSuccessStatusCode)
            {
                // If the API rejects it, capture the reason (e.g., 400 Bad Request)
                var errorBody = await response.Content.ReadAsStringAsync();
                queueItem.VchErrorLog = $"API Error ({response.StatusCode}): {errorBody}";
                return false;
            }

            return true; // Success! Record will turn Green.
        }
        catch (Exception ex)
        {
            // Capture network timeouts or "Connection Refused" errors
            queueItem.VchErrorLog = $"Network Error: {ex.Message}";
            return false;
        }
    }

    private void UpdateRegistryTimestamp(TblServerRegistry server, string module)
    {
        if (module == "CARD") server.DtLastCardSync = DateTime.Now;
        else if (module == "CORP") server.DtLastCorpSync = DateTime.Now;
        else if (module == "MASTER") server.DtLastMasterSync = DateTime.Now;
        server.DtLastSync = DateTime.Now;
        server.BitIsActive = true;
    }
}