using Animal.Domain.Entities;

namespace Animal.API.Services
{
    // Planned application service, not implemented yet. When you build it, follow IPetService/PetService:
    // async methods, request/response DTOs from Contracts/ instead of entities, and repositories from Animal.Data.
    public interface IAdoptionService
    {
        AdoptionApplication GetApplication(int id);
        List<AdoptionApplication> GetApplications();
        AdoptionApplication CreateApplication(AdoptionApplication application);
        void ApproveApplication(int id);
        void RejectApplication(int id);
    }
}
