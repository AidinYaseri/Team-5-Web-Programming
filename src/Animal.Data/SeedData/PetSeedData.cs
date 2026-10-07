using Animal.Domain.Models;

namespace Animal.Data.SeedData
{
    // Fake pets used until the EF Core database exists. Not real animals or shelters.
    public static class PetSeedData
    {
        public static List<Pet> GetPets() =>
        [
            new Pet { Id = 1, Name = "Biscuit", Species = "Dog", Breed = "Beagle", Age = 3, IsAvailable = true, ShelterId = 1 },
            new Pet { Id = 2, Name = "Mochi", Species = "Cat", Breed = "Siamese", Age = 1, IsAvailable = true, ShelterId = 1 },
            new Pet { Id = 3, Name = "Rex", Species = "Dog", Breed = "German Shepherd", Age = 7, IsAvailable = false, ShelterId = 2 },
            new Pet { Id = 4, Name = "Pepper", Species = "Rabbit", Breed = "Holland Lop", Age = 2, IsAvailable = true, ShelterId = 2 },
            new Pet { Id = 5, Name = "Luna", Species = "Cat", Breed = "Domestic Shorthair", Age = 9, IsAvailable = true, ShelterId = 1 }
        ];
    }
}
