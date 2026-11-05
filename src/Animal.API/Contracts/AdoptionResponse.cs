using Animal.Domain.Entities;

namespace Animal.API.Contracts
{
    // Status is sent as text ("Pending", "Approved", "Rejected"), like appointments.
    public record AdoptionResponse(int Id, int PetId, string PetName, int UserId, string Status, DateTime ApplicationDate)
    {
        public static AdoptionResponse FromApplication(AdoptionApplication application, string petName) =>
            new(application.Id, application.PetId, petName, application.UserId, application.Status.ToString(), application.ApplicationDate);
    }
}
