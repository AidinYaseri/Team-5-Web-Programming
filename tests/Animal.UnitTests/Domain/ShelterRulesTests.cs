using Animal.Domain.Entities;
using Animal.Domain.Exceptions;
using Animal.Domain.Services;

namespace Animal.UnitTests.Domain
{
    public class ShelterRulesTests
    {
        private static Shelter ValidShelter() => new()
        {
            Id = 1,
            Name = "Maple Street Animal Shelter",
            Address = "123 Maple Street",
            PhoneNumber = "+1 (555) 0100",
            Email = "contact@maple-shelter.example"
        };

        [Fact]
        public void Validate_ValidShelter_ReturnsNoErrors()
        {
            Assert.Empty(ShelterRules.Validate(ValidShelter()));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Validate_BlankName_ReturnsError(string name)
        {
            var shelter = ValidShelter();
            shelter.Name = name;

            Assert.Contains("Name is required.", ShelterRules.Validate(shelter));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void Validate_BlankAddress_ReturnsError(string address)
        {
            var shelter = ValidShelter();
            shelter.Address = address;

            Assert.Contains("Address is required.", ShelterRules.Validate(shelter));
        }

        [Theory]
        [InlineData("bad-email")]
        [InlineData("test@")]
        public void Validate_InvalidEmail_ReturnsError(string email)
        {
            var shelter = ValidShelter();
            shelter.Email = email;

            Assert.Contains("Email format is invalid.", ShelterRules.Validate(shelter));
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("123")]
        public void Validate_InvalidPhone_ReturnsError(string phone)
        {
            var shelter = ValidShelter();
            shelter.PhoneNumber = phone;

            Assert.Contains("Phone number format is invalid.", ShelterRules.Validate(shelter));
        }

        [Fact]
        public void EnsureValid_MultipleFailures_ThrowsWithAllErrors()
        {
            var shelter = new Shelter { Name = "", Address = "", Email = "bad", PhoneNumber = "x" };

            var ex = Assert.Throws<DomainValidationException>(() => ShelterRules.EnsureValid(shelter));

            Assert.Equal(4, ex.Errors.Count);
        }
    }
}