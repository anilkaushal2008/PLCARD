using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace PLCARD.Models;

[ModelMetadataType(typeof(TblCompanyRegistrationMetadata))]
public partial class TblCompanyRegistration { }

public class TblCompanyRegistrationMetadata
{
    [Required(ErrorMessage = "Company Name is mandatory.*")]
    [Display(Name = "Company Name")]
    [StringLength(200)]
    public string VchCompanyName { get; set; } = null!;

    [Required(ErrorMessage = "Please select a Corporate Plan.*")]
    public int IntPlanId { get; set; }

    [Required(ErrorMessage = "Contact Person is required.*")]
    public string VchContactPerson { get; set; }

    [Required(ErrorMessage = "Contact Number is required.*")]
    [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Invalid 10-digit mobile number.*")]
    public string VchContactNo { get; set; }

    [EmailAddress(ErrorMessage = "Invalid Email Address.*")]
    public string VchEmail { get; set; }

    [RegularExpression(@"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$", ErrorMessage = "Invalid GST Format.*")]
    public string VchGstNo { get; set; }

    [RegularExpression(@"^[A-Z]{5}[0-9]{4}[A-Z]{1}$", ErrorMessage = "Invalid PAN Format.*")]
    public string VchPanNo { get; set; }

    [RegularExpression(@"^[1-9][0-9]{5}$", ErrorMessage = "Invalid Pincode (6 digits).*")]
    public string VchPincode { get; set; }
}