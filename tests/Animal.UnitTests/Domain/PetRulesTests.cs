using Animal.Domain.Entities;
using Animal.Domain.Exceptions;
using Animal.Domain.Services;

namespace Animal.UnitTests.Domain
{
    public class PetRulesTests
    {
        private static Pet ValidPet() =>
            new() { Id = 1, Name = "Biscuit", Species = "Dog", Breed = "Beagle", Age = 3, IsAvailable = true, ShelterId = 1 };

        [Fact]
        public void Validate_ValidPet_ReturnsNoErrors()
        {
            Assert.Empty(PetRules.Validate(ValidPet()));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Validate_BlankName_ReturnsError(string name)
        {
            var pet = ValidPet();
            pet.Name = name;

            Assert.Contains("Name is required.", PetRules.Validate(pet));
        }

        [Fact]
        public void Validate_BlankSpecies_ReturnsError()
        {
            var pet = ValidPet();
            pet.Species = "";

            Assert.Contains("Species is required.", PetRules.Validate(pet));
        }

        [Fact]
        public void Validate_SpeciesAtMaxLength_IsAllowed()
        {
            var pet = ValidPet();
            pet.Species = new string('a', PetRules.MaxSpeciesLength);

            Assert.Empty(PetRules.Validate(pet));
        }

        [Fact]
        public void Validate_SpeciesTooLong_ReturnsError()
        {
            var pet = ValidPet();
            pet.Species = new string('a', PetRules.MaxSpeciesLength + 1);

            Assert.Single(PetRules.Validate(pet));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(100)]
        public void Validate_AgeOnTheBoundary_IsAllowed(int age)
        {
            var pet = ValidPet();
            pet.Age = age;

            Assert.Empty(PetRules.Validate(pet));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(101)]
        public void Validate_AgeOutOfRange_ReturnsError(int age)
        {
            var pet = ValidPet();
            pet.Age = age;

            Assert.Contains("Age must be between 0 and 100.", PetRules.Validate(pet));
        }

        [Fact]
        public void Validate_NoShelter_ReturnsError()
        {
            var pet = ValidPet();
            pet.ShelterId = 0;

            Assert.Contains("A pet must belong to a shelter.", PetRules.Validate(pet));
        }

        [Fact]
        public void EnsureValid_ReportsEveryBrokenRule()
        {
            var pet = new Pet { Name = "", Species = "", Age = -1, ShelterId = 0 };

            var ex = Assert.Throws<DomainValidationException>(() => PetRules.EnsureValid(pet));

            Assert.Equal(4, ex.Errors.Count);
        }

        [Fact]
        public void EnsureValid_ValidPet_DoesNotThrow()
        {
            PetRules.EnsureValid(ValidPet());
        }
    }
}
