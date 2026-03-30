using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace PLCARD.Models;

[ModelMetadataType(typeof(TblCardRegistrationMetadata))]
public partial class TblCardRegistration
{
    // This partial class remains empty to link the metadata.
}

public class TblCardRegistrationMetadata
{
    [Required(ErrorMessage = "Please select a Card Type.*")]
    [Display(Name = "Card Plan")]
    public int IntCardId { get; set; }

    [Required(ErrorMessage = "UHID Number is mandatory.*")]
    [Display(Name = "UHID No")]
    public string VchUhidno { get; set; }

    [Required(ErrorMessage = "Patient Name is mandatory.*")]
    [Display(Name = "Patient Name")]
    public string Vchname { get; set; }

    [Required(ErrorMessage = "Gender selection is required.*")]
    [Display(Name = "Gender")]
    public string Vchsex { get; set; }

    [Required(ErrorMessage = "Age is mandatory.*")]
    [Range(0, 120, ErrorMessage = "Enter a valid age (0-120).*")]
    [Display(Name = "Age")]
    public int? Intage { get; set; }

    [Required(ErrorMessage = "Contact Number is required.*")]
    [Phone(ErrorMessage = "Invalid phone format.*")]
    [Display(Name = "Contact No")]
    public string Vchcontactno { get; set; }

    [EmailAddress(ErrorMessage = "Invalid Email address format.*")]
    [Display(Name = "Email ID")]
    public string Vchemail { get; set; }
}