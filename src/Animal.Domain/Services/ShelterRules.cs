using System.Net.Mail;
using System.Text.RegularExpressions;
using Animal.Domain.Entities;
using Animal.Domain.Exceptions;

namespace Animal.Domain.Services
{
    public static partial class ShelterRules
    {
        public const int MaxNameLength = 100;
        public const int MaxAddressLength = 200;
        public const int MaxPhoneLength = 20;
        public const int MaxEmailLength = 254;

        public static IReadOnlyList<string> Validate(Shelter shelter)
        {
            ArgumentNullException.ThrowIfNull(shelter);

            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(shelter.Name))
            {
                errors.Add("Name is required.");
            }
            else if (shelter.Name.Length > MaxNameLength)
            {
                errors.Add($"Name can't be longer than {MaxNameLength} characters.");
            }

            if (string.IsNullOrWhiteSpace(shelter.Address))
            {
                errors.Add("Address is required.");
            }
            else if (shelter.Address.Length > MaxAddressLength)
            {
                errors.Add($"Address can't be longer than {MaxAddressLength} characters.");
            }

            if (string.IsNullOrWhiteSpace(shelter.Email))
            {
                errors.Add("Email is required.");
            }
            else if (shelter.Email.Length > MaxEmailLength)
            {
                errors.Add($"Email can't be longer than {MaxEmailLength} characters.");
            }
            else if (!IsValidEmail(shelter.Email))
            {
                errors.Add("Email format is invalid.");
            }

            if (string.IsNullOrWhiteSpace(shelter.PhoneNumber))
            {
                errors.Add("Phone number is required.");
            }
            else if (shelter.PhoneNumber.Length > MaxPhoneLength)
            {
                errors.Add($"Phone number can't be longer than {MaxPhoneLength} characters.");
            }
            else if (!PhoneRegex().IsMatch(shelter.PhoneNumber))
            {
                errors.Add("Phone number format is invalid.");
            }

            return errors;
        }

        public static void EnsureValid(Shelter shelter)
        {
            var errors = Validate(shelter);
            if (errors.Count > 0)
            {
                throw new DomainValidationException(errors);
            }
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                _ = new MailAddress(email);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        [GeneratedRegex(@"^\+?[0-9()\-\s.]{7,20}$")]
        private static partial Regex PhoneRegex();
    }
}