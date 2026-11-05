using Animal.Domain.Entities;
using Animal.Domain.Exceptions;
using Animal.Domain.Queries;

namespace Animal.Domain.Services
{
    // Pure business rules for a pet. No database or HTTP here, only checks on the object in memory.
    public static class PetRules
    {
        public const int MaxNameLength = 50;
        public const int MaxSpeciesLength = 50;
        public const int MaxBreedLength = 50;
        public const int MinAge = 0;
        public const int MaxAge = 100;

        // Species matching is exact, case-insensitive, and ignores surrounding spaces.
        public static bool MatchesSpecies(Pet pet, string? species)
        {
            ArgumentNullException.ThrowIfNull(pet);
            var trimmed = species?.Trim();
            return string.IsNullOrEmpty(trimmed)
                || string.Equals(pet.Species, trimmed, StringComparison.OrdinalIgnoreCase);
        }

        public static bool Matches(Pet pet, PetSearchCriteria criteria)
        {
            ArgumentNullException.ThrowIfNull(pet);
            ArgumentNullException.ThrowIfNull(criteria);

            return MatchesSpecies(pet, criteria.Species)
                && (!criteria.MaxAge.HasValue || pet.Age <= criteria.MaxAge.Value)
                && (!criteria.AvailableOnly || pet.IsAvailable);
        }

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

            if (pet.Age < MinAge || pet.Age > MaxAge)
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
