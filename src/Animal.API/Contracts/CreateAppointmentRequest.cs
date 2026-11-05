using System.ComponentModel.DataAnnotations;

namespace Animal.API.Contracts
{
    public sealed class CreateAppointmentRequest
    {
        [Range(1, int.MaxValue)]
        public int PetId { get; set; }

        [Required]
        public DateTimeOffset? AppointmentDate { get; set; }
    }
}
