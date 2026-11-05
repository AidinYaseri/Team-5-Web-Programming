using System.ComponentModel.DataAnnotations;

namespace Animal.Client.Contracts
{
    public sealed class CreateAppointmentRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "Choose a pet.")]
        public int PetId { get; set; }

        [Required(ErrorMessage = "Choose a visit date and time.")]
        public DateTime? AppointmentDate { get; set; }
    }
}
