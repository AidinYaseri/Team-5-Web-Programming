using Animal.Domain.Enums;

namespace Animal.Domain.Entities
{
    public class Appointment
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int PetId { get; set; }
        public int ShelterId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
    }
}
