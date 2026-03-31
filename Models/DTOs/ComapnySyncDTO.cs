using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PLCARD.Models.DTOs
{
    public class ComapnySyncDTO
    {
        [JsonIgnore] // Not needed in the incoming JSON request
        public int? Status { get; set; }

        [JsonIgnore] // Not needed in the incoming JSON request
        public string? Message { get; set; }

        public int IntCompanyId { get; set; }

        [Required]
        public string VchCompanyName { get; set; } = null!;

        public int? IntPlanId { get; set; }
        public string? VchContactPerson { get; set; }
        public string? VchContactNo { get; set; }
        public string? VchEmail { get; set; }
        public string? VchGstNo { get; set; }
        public string? VchPanNo { get; set; }
        public string? VchPincode { get; set; }

        // EF Core requires this property if SQL returns it
        public DateTime? DtRegistration { get; set; }
    }
}
