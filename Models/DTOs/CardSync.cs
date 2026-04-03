using System.ComponentModel.DataAnnotations;

namespace PLCARD.Models.DTOs
{
    public class CardSync
    {
        public int IntRegId { get; set; }
        public int IntCardId { get; set; }

        [StringLength(100)]
        public string? VchHmsRcpt { get; set; }

        [StringLength(100)]
        public string? VchCardType { get; set; }

        [Required]
        [StringLength(50)]
        public string VchUhidno { get; set; } = null!;

        [Required]
        [StringLength(200)]
        public string Vchname { get; set; } = null!;

        public DateTime? DtDob { get; set; }

        [StringLength(20)]
        public string? Vchsex { get; set; }

        public int? Intage { get; set; }

        [StringLength(200)]
        public string? VchspouseName { get; set; }

        [StringLength(200)]
        public string? Vchemail { get; set; }

        [StringLength(100)]
        public string? VchsState { get; set; }

        [StringLength(100)]
        public string? Vchcity { get; set; }

        public string? VchAddress { get; set; }

        [StringLength(20)]
        public string? Vchcontactno { get; set; }

        public int? IntCharges { get; set; }

        [StringLength(20)]
        public string? Vchpincode { get; set; }

        [StringLength(100)]
        public string? VchCardRefBy { get; set; }

        public int? FkBId { get; set; } // Essential for the IHMS master audit
    }
}