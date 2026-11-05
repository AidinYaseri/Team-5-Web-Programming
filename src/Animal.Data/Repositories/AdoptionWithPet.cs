using Animal.Domain.Entities;

namespace Animal.Data.Repositories
{
    // An application together with its pet's name, read in one query for list and detail screens.
    public sealed record AdoptionWithPet(AdoptionApplication Application, string PetName);
}
