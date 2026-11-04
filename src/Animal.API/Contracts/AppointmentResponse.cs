using Animal.Domain.Entities;

namespace Animal.API.Contracts
{
    public record AppointmentResponse(int Id, int PetId, int ShelterId, DateTime AppointmentDate, string Status)
    {
        public static AppointmentResponse FromEntity(Appointment appointment) =>
            new(appointment.Id, appointment.PetId, appointment.ShelterId, appointment.AppointmentDate, appointment.Status.ToString());
    }
}
