using Animal.Domain.Exceptions;

namespace Animal.Domain.Services
{
    public static class ProfilePictureRules
    {
        public const int MaxBytes = 2 * 1024 * 1024;

        private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        private static readonly byte[] JpegStart = [0xFF, 0xD8];
        private static readonly byte[] JpegEnd = [0xFF, 0xD9];

        public static IReadOnlyList<string> Validate(string? declaredContentType, byte[]? bytes)
        {
            var errors = new List<string>();

            if (bytes is null || bytes.Length == 0)
            {
                errors.Add("Profile picture is required.");
                return errors;
            }

            if (bytes.Length > MaxBytes)
            {
                errors.Add($"Profile picture can't be larger than {MaxBytes} bytes.");
            }

            var actualContentType = DetectContentType(bytes);
            if (actualContentType is null)
            {
                errors.Add("Only PNG and JPEG profile pictures are allowed.");
                return errors;
            }

            if (string.IsNullOrWhiteSpace(declaredContentType))
            {
                errors.Add("Content type is required.");
                return errors;
            }

            if (!string.Equals(declaredContentType.Trim(), actualContentType, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("Declared content type does not match the file signature.");
            }

            return errors;
        }

        public static void EnsureValid(string? declaredContentType, byte[]? bytes)
        {
            var errors = Validate(declaredContentType, bytes);
            if (errors.Count > 0)
            {
                throw new DomainValidationException(errors);
            }
        }

        public static string? DetectContentType(byte[]? bytes)
        {
            if (bytes is null || bytes.Length == 0)
            {
                return null;
            }

            if (HasPrefix(bytes, PngSignature))
            {
                return "image/png";
            }

            if (IsJpeg(bytes))
            {
                return "image/jpeg";
            }

            return null;
        }

        private static bool HasPrefix(byte[] bytes, byte[] prefix)
        {
            if (bytes.Length < prefix.Length)
            {
                return false;
            }

            for (var i = 0; i < prefix.Length; i++)
            {
                if (bytes[i] != prefix[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsJpeg(byte[] bytes)
        {
            if (bytes.Length < 4)
            {
                return false;
            }

            return HasPrefix(bytes, JpegStart)
                && bytes[bytes.Length - 2] == JpegEnd[0]
                && bytes[bytes.Length - 1] == JpegEnd[1];
        }
    }
}
