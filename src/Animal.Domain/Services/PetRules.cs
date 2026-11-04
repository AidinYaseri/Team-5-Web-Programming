using Animal.Domain.Entities;
using Animal.Domain.Exceptions;

namespace Animal.Domain.Services
{
    // Pure business rules for a pet. No database or HTTP here, only checks on the object in memory.
    public static class PetRules
    {
        public const int MaxNameLength = 100;
        public const int MaxSpeciesLength = 50;
        public const int MaxBreedLength = 100;
        public const int MaxAge = 100;

        public static IReadOnlyList<string> Validate(Pet pet)
        {
            ArgumentNullException.ThrowIfNull(pet);

            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(pet.Name))
            {
                errors.Add("Name is required.");
            }
            else if (pet.Name.Length > MaxNameLength)
            {
                errors.Add($"Name can't be longer than {MaxNameLength} characters.");
            }

            if (string.IsNullOrWhiteSpace(pet.Species))
            {
                errors.Add("Species is required.");
            }
            else if (pet.Species.Length > MaxSpeciesLength)
            {
                errors.Add($"Species can't be longer than {MaxSpeciesLength} characters.");
            }

            if (pet.Breed.Length > MaxBreedLength)
            {
                errors.Add($"Breed can't be longer than {MaxBreedLength} characters.");
            }

            if (pet.Age < 0 || pet.Age > MaxAge)
            {
                errors.Add($"Age must be between 0 and {MaxAge}.");
            }

            if (pet.ShelterId <= 0)
            {
                errors.Add("A pet must belong to a shelter.");
            }

            return errors;
        }

        public static void EnsureValid(Pet pet)
        {
            var errors = Validate(pet);
            if (errors.Count > 0)
            {
                throw new DomainValidationException(errors);
            }
        }
    }
}
