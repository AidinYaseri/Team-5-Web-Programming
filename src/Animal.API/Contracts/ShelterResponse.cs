using Animal.Domain.Entities;

namespace Animal.API.Contracts
{
    public record ShelterResponse(int Id, string Name, string Address, string PhoneNumber, string Email)
    {
        public static ShelterResponse FromShelter(Shelter shelter) =>
            new(shelter.Id, shelter.Name, shelter.Address, shelter.PhoneNumber, shelter.Email);
    }
}