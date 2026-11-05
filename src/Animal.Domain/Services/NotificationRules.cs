using Animal.Domain.Exceptions;

namespace Animal.Domain.Services
{
    public static class NotificationRules
    {
        public const int MaximumMessageLength = 500;

        public static string ValidateMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new DomainValidationException("A notification message is required.");
            }

            var trimmed = message.Trim();
            if (trimmed.Length > MaximumMessageLength)
            {
                throw new DomainValidationException($"A notification message can't exceed {MaximumMessageLength} characters.");
            }

            return trimmed;
        }
    }
}
