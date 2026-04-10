using DWBAPI.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using PLCARD.Data;
using PLCARD.Models;
using PLCARD.Models.DTOs;
using System.Net.Http.Json;
using Microsoft.Data.SqlClient;

namespace PLCARD.Services;

public class GlobalSyncWorker(IServiceProvider serviceProvider, IHttpClientFactory httpClientFactory, ILogger<GlobalSyncWorker> logger, IConfiguration configuration) : BackgroundService
{
    private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Global Sync Worker (Dynamic Master-Detail) started.");

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

            // Sync interval: 1 minute
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task ProcessSyncQueue(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PLCARDContext>();
        var http = httpClientFactory.CreateClient();

        // 1. Fetch pending items from the queue
        // Note: Using IntServerId to match your TblSyncQueue model
        var pendingItems = await context.TblSyncQueue
            .Where(q => q.BitProcessed != true && q.IntRetryCount < 5)
            .OrderBy(q => q.VchModule == "MASTER" ? 0 : 1)
            .ToListAsync(ct);

        if (!pendingItems.Any()) return;

        // Grouping by the Foreign Key to ServerMaster
        var groupedByServer = pendingItems.GroupBy(q => q.IntServerId);

        foreach (var serverGroup in groupedByServer)
        {
            // Use the new ServerMaster DbSet from your Context
            var server = await context.ServerMaster.FindAsync(serverGroup.Key);

            if (server == null || server.BitIsActive != true) continue;

            // 2. FETCH DYNAMIC URL
            // We fetch the 'Master Data Sync' endpoint (Assuming ServiceTypeId = 1)
            string syncUrl = await GetEndpointUrl(server.IntServerId, 1);

            if (string.IsNullOrEmpty(syncUrl))
            {
                logger.LogWarning($"Hub '{server.VchServerName}' has no Sync URL configured in TblHubServiceEndpoints.");
                continue;
            }

            foreach (var item in serverGroup)
            {
                // Push data to the dynamically retrieved URL
                bool success = await PushRecordToRemote(syncUrl, item, context, http);

                if (success)
                {
                    item.BitProcessed = true;
                    UpdateRegistryTimestamp(server);
                }
                else
                {
                    item.IntRetryCount++;
                }
            }
            await context.SaveChangesAsync(ct);
        }
    }

    private async Task<string> GetEndpointUrl(int serverId, int serviceTypeId)
    {
        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        // Query the new Mapping Table
        var cmd = new SqlCommand("SELECT VchEndpointUrl FROM TblHubServiceEndpoints WHERE IntServerId = @SId AND ServiceTypeId = @STId", conn);
        cmd.Parameters.AddWithValue("@SId", serverId);
        cmd.Parameters.AddWithValue("@STId", serviceTypeId);

        var result = await cmd.ExecuteScalarAsync();
        return result?.ToString() ?? string.Empty;
    }

    private async Task<bool> PushRecordToRemote(string apiUrl, TblSyncQueue queueItem, PLCARDContext db, HttpClient http)
    {
        try
        {
            var gatewayRequest = new SyncGatewayRequest();

            if (queueItem.VchModule == "CORP")
            {
                var company = await db.TblCompanyRegistration.FindAsync(queueItem.IntRecordId);
                if (company == null) return true;
                gatewayRequest.SyncType = "COMPANY";
                gatewayRequest.CompanyData = new ComapnySyncDTO
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
                var card = await db.TblCardRegistration.FindAsync(queueItem.IntRecordId);
                if (card == null) return true;
                gatewayRequest.SyncType = "CARD";
                gatewayRequest.CardData = new LiteCardSyncDTO
                {
                    IntRegId = card.IntRegId,
                    IntCardId = card.IntCardId,
                    VchHmsRcpt = card.VchHmsRcpt,
                    VchCardType = card.VchCardType,
                    VchUhidno = card.VchUhidno,
                    Vchname = card.Vchname
                };
            }

            if (string.IsNullOrEmpty(gatewayRequest.SyncType)) return true;

            http.DefaultRequestHeaders.Clear();
            // Optional: If you add VchApiKey to ServerMaster later, add it here

            var response = await http.PostAsJsonAsync(apiUrl, gatewayRequest);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                queueItem.VchErrorLog = $"Sync Error ({response.StatusCode}): {errorBody}";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            queueItem.VchErrorLog = $"Network Error: {ex.Message}";
            return false;
        }
    }

    private void UpdateRegistryTimestamp(ServerMaster server)
    {
        // Update general status (Old module-specific columns are deleted)
        server.BitIsActive = true;
        // If you added a DtLastSync column to ServerMaster, update it here:
        // server.DtLastSync = DateTime.Now;
    }
}