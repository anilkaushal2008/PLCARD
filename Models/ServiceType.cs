namespace PLCARD.Models
{
    public class ServiceType
    {
        public int ServiceTypeId { get; set; }
        public string? ServiceName { get; set; }
        public string? DefaultMethod { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }
}