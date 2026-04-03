using System.ComponentModel.DataAnnotations;

namespace DWBAPI.Models.DTOs
{
    public class LiteCardSyncDTO
    {
        [Required]
        public int IntRegId { get; set; }

        [Required]
        public int IntCardId { get; set; }

        [StringLength(100)]
        public string? VchHmsRcpt { get; set; }

        [StringLength(100)]
        public string? VchCardType { get; set; }

        [StringLength(50)]
        public string? VchUhidno { get; set; }

        [StringLength(200)]
        public string? Vchname { get; set; }

        // Optional: Add Branch ID if you decide to track hospital location
        // public int? FkBId { get; set; } 
    }
}