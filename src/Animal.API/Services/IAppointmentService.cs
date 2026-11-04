using Animal.Domain.Entities;

namespace Animal.API.Services
{
    // Planned application service, not implemented yet. When you build it, follow IPetService/PetService:
    // async methods, request/response DTOs from Contracts/ instead of entities, and repositories from Animal.Data.
    public interface IAppointmentService
    {
        Appointment GetAppointment(int id);
        List<Appointment> GetAppointments();
        Appointment CreateAppointment(Appointment appointment);
        void UpdateAppointment(Appointment appointment);
        void CancelAppointment(int id);
    }
}
