using DWBAPI.Models.DTOs; // Ensure you have access to your DTO namespace

    namespace PLCARD.Models.DTOs
    {
        public class SyncGatewayRequest
        {
            /// <summary>
            /// Defines the payload type: "COMPANY" or "CARD"
            /// </summary>
            public string SyncType { get; set; }

            /// <summary>
            /// Data for Company Registrations
            /// </summary>
            public ComapnySyncDTO? CompanyData { get; set; }

            /// <summary>
            /// Data for the "Lite" Card Audit Sync
            /// </summary>
            public LiteCardSyncDTO? CardData { get; set; }
        }
    }