using Animal.Domain.Entities;

namespace Animal.Data.SeedData
{
    // Fake shelters for the seed pets to belong to. Not real organizations or contact details.
    public static class ShelterSeedData
    {
        public static List<Shelter> GetShelters() =>
        [
            new Shelter { Id = 1, Name = "Maple Street Animal Shelter", Address = "123 Maple Street, Springfield", PhoneNumber = "555-0100", Email = "contact@maple-shelter.example" },
            new Shelter { Id = 2, Name = "Riverside Pet Rescue", Address = "45 River Road, Springfield", PhoneNumber = "555-0101", Email = "hello@riverside-rescue.example" }
        ];
    }
}
