using System;

namespace PLCARD.Models;

public class CardRegistrationReportResult
{
    public int IntRegId { get; set; }
    public DateTime? Dtcreated { get; set; }
    public string? VchUhidno { get; set; }
    public string? Vchname { get; set; }
    public string? VchCardType { get; set; }
    public string? Vchcontactno { get; set; }
    public string? Vchsex { get; set; }
    public int? Intage { get; set; }
    public int? IntCharges { get; set; }
    public string? Vchcity { get; set; }
}