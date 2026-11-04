using Animal.Domain.Entities;

namespace Animal.API.Services
{
    // Planned application service, not implemented yet. When you build it, follow IPetService/PetService:
    // async methods, request/response DTOs from Contracts/ instead of entities, and repositories from Animal.Data.
    public interface IShelterService
    {
        Shelter GetShelter(int id);
        List<Shelter> GetShelters();
        Shelter CreateShelter(Shelter shelter);
        void UpdateShelter(Shelter shelter);
        void DeleteShelter(int id);
    }
}
