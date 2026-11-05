using Animal.Domain.Entities;
using Animal.Domain.Exceptions;
using Animal.Domain.Services;

namespace Animal.UnitTests.Domain
{
    public class UserRulesTests
    {
        private static User ValidUser() =>
            new()
            {
                ExternalId = "subject-1",
                Name = "Sam User",
                Email = "sam@example.com",
                PhoneNumber = "555-0100"
            };

        [Fact]
        public void Validate_ValidUser_ReturnsNoErrors()
        {
            Assert.Empty(UserRules.Validate(ValidUser()));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Validate_BlankRequiredValue_ReturnsError(string value)
        {
            var user = ValidUser();
            user.Name = value;

            Assert.Contains("Name is required.", UserRules.Validate(user));
        }

        [Theory]
        [InlineData("ExternalId", 255)]
        [InlineData("Name", 100)]
        [InlineData("Email", 254)]
        [InlineData("PhoneNumber", 20)]
        public void Validate_ValueAtMaximumLength_IsAllowed(string property, int length)
        {
            var user = ValidUser();
            var value = new string('a', length);

            switch (property)
            {
                case "ExternalId":
                    user.ExternalId = value;
                    break;
                case "Name":
                    user.Name = value;
                    break;
                case "Email":
                    user.Email = "a".PadRight(length - 10, 'a') + "@a.com";
                    break;
                case "PhoneNumber":
                    user.PhoneNumber = value;
                    break;
            }

            Assert.Empty(UserRules.Validate(user));
        }

        [Fact]
        public void Validate_NameTooLong_ReturnsError()
        {
            var user = ValidUser();
            user.Name = new string('a', UserRules.MaxNameLength + 1);

            Assert.Contains("Name can't be longer than 100 characters.", UserRules.Validate(user));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Validate_BlankEmail_ReturnsRequiredError(string email)
        {
            var user = ValidUser();
            user.Email = email;

            Assert.Contains("Email is required.", UserRules.Validate(user));
        }

        [Fact]
        public void Validate_MalformedNonBlankEmail_ReturnsEmailError()
        {
            var user = ValidUser();
            user.Email = "not-an-email";

            Assert.Contains("Email must be a valid email address.", UserRules.Validate(user));
        }

        [Fact]
        public void EnsureValid_InvalidUser_ThrowsDomainValidationException()
        {
            var user = ValidUser();
            user.ExternalId = "";
            user.Email = "bad";

            var exception = Assert.Throws<DomainValidationException>(() => UserRules.EnsureValid(user));

            Assert.Contains("ExternalId is required.", exception.Errors);
            Assert.Contains("Email must be a valid email address.", exception.Errors);
        }
    }
}
