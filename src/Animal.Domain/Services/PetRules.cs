using Animal.Domain.Entities;
using Animal.Domain.ValueObjects;

namespace Animal.Domain.Services
{
    // Pure pet rules with no I/O, so they can be unit tested and reused by any repository.
    public static class PetRules
    {
        public const int MinAge = 0;
        public const int MaxAge = 100;

        // Species match is exact but ignores case and surrounding spaces (decided in Milestone 2, issue #7).
        public static bool MatchesSpecies(Pet pet, string? species)
        {
            var trimmed = species?.Trim();
            return string.IsNullOrEmpty(trimmed)
                || string.Equals(pet.Species, trimmed, StringComparison.OrdinalIgnoreCase);
        }

        public static bool Matches(Pet pet, PetSearchCriteria criteria)
        {
            ArgumentNullException.ThrowIfNull(criteria);

            return MatchesSpecies(pet, criteria.Species)
                && (!criteria.MaxAge.HasValue || pet.Age <= criteria.MaxAge.Value)
                && (!criteria.AvailableOnly || pet.IsAvailable);
        }
    }
}
