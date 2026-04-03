using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PLCARD.Models.DTOs
{
    public class ComapnySyncDTO
    {
        public int IntCompanyId { get; set; }

        [Required]
        [StringLength(200)]
        public string VchCompanyName { get; set; } = null!;

        public int? IntPlanId { get; set; }

        [StringLength(100)]
        public string? VchContactPerson { get; set; }

        [StringLength(20)] // Must match API's increased length
        public string? VchContactNo { get; set; }

        [StringLength(100)]
        public string? VchEmail { get; set; }

        [StringLength(20)] // Must match API's increased length
        public string? VchGstNo { get; set; }

        [StringLength(20)]
        public string? VchPanNo { get; set; }

        [StringLength(10)]
        public string? VchPincode { get; set; }

        public DateTime? DtRegistration { get; set; }
    }
}
