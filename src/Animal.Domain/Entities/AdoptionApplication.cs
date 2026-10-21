using Animal.Domain.Enums;

namespace Animal.Domain.Entities
{
    public class AdoptionApplication
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int PetId { get; set; }
        public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;
        public DateTime ApplicationDate { get; set; }
    }
}
