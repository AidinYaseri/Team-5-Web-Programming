using Animal.Domain.Enums;

namespace Animal.Domain.Entities
{
    public class AdoptionApplication
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int PetId { get; set; }
        public AdoptionStatus Status { get; set; } = AdoptionStatus.Pending;
        public DateTime ApplicationDate { get; set; }
    }
}
