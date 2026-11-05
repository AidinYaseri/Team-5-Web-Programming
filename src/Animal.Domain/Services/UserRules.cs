using System.Text.RegularExpressions;
using Animal.Domain.Entities;
using Animal.Domain.Exceptions;

namespace Animal.Domain.Services
{
    public static partial class UserRules
    {
        public const int MaxExternalIdLength = 255;
        public const int MaxNameLength = 100;
        public const int MaxEmailLength = 254;
        public const int MaxPhoneNumberLength = 20;

        public static IReadOnlyList<string> Validate(User user)
        {
            ArgumentNullException.ThrowIfNull(user);

            var errors = new List<string>();

            AddRequiredAndLengthError(errors, user.ExternalId, "ExternalId", MaxExternalIdLength);
            AddRequiredAndLengthError(errors, user.Name, "Name", MaxNameLength);
            AddRequiredAndLengthError(errors, user.Email, "Email", MaxEmailLength);
            AddRequiredAndLengthError(errors, user.PhoneNumber, "Phone number", MaxPhoneNumberLength);

            if (!string.IsNullOrWhiteSpace(user.Email)
                && user.Email.Length <= MaxEmailLength
                && !EmailPattern().IsMatch(user.Email))
            {
                errors.Add("Email must be a valid email address.");
            }

            return errors;
        }

        public static void EnsureValid(User user)
        {
            var errors = Validate(user);
            if (errors.Count > 0)
            {
                throw new DomainValidationException(errors);
            }
        }

        private static void AddRequiredAndLengthError(
            ICollection<string> errors,
            string value,
            string fieldName,
            int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add($"{fieldName} is required.");
            }
            else if (value.Length > maxLength)
            {
                errors.Add($"{fieldName} can't be longer than {maxLength} characters.");
            }
        }

        [GeneratedRegex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.CultureInvariant)]
        private static partial Regex EmailPattern();
    }
}
