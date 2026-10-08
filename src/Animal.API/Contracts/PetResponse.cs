using Animal.Domain.Models;

namespace Animal.API.Contracts
{
    public record PetResponse(int Id, string Name, string Species, string Breed, int Age, bool IsAvailable, int ShelterId)
    {
        public static PetResponse FromPet(Pet pet) =>
            new(pet.Id, pet.Name, pet.Species, pet.Breed, pet.Age, pet.IsAvailable, pet.ShelterId);
    }
}
