using Animal.Domain.Entities;
using Animal.Domain.Services;
using Animal.Domain.ValueObjects;

namespace Animal.Domain.Tests
{
    public class PetRulesTests
    {
        private static readonly Pet Biscuit = new() { Id = 1, Name = "Biscuit", Species = "Dog", Age = 3, IsAvailable = true };
        private static readonly Pet Rex = new() { Id = 3, Name = "Rex", Species = "Dog", Age = 7, IsAvailable = false };

        [Theory]
        [InlineData("dog", true)]
        [InlineData("  DOG ", true)]
        [InlineData("Do", false)]
        [InlineData("Cat", false)]
        [InlineData(null, true)]
        [InlineData("   ", true)]
        public void MatchesSpecies_IsExactButIgnoresCaseAndSpaces(string? species, bool expected)
        {
            Assert.Equal(expected, PetRules.MatchesSpecies(Biscuit, species));
        }

        [Fact]
        public void Matches_MaxAgeIsInclusive()
        {
            Assert.True(PetRules.Matches(Biscuit, new PetSearchCriteria { MaxAge = 3 }));
            Assert.False(PetRules.Matches(Biscuit, new PetSearchCriteria { MaxAge = 2 }));
        }

        [Fact]
        public void Matches_AvailableOnly_ExcludesAdoptedPets()
        {
            var criteria = new PetSearchCriteria { AvailableOnly = true };

            Assert.True(PetRules.Matches(Biscuit, criteria));
            Assert.False(PetRules.Matches(Rex, criteria));
        }

        [Fact]
        public void Matches_WithNullCriteria_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => PetRules.Matches(Biscuit, null!));
        }
    }
}
