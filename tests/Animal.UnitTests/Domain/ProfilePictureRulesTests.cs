using System.Text;
using Animal.Domain.Exceptions;
using Animal.Domain.Services;

namespace Animal.UnitTests.Domain
{
    public class ProfilePictureRulesTests
    {
        private static readonly byte[] PngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00];
        private static readonly byte[] JpegBytes = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0xFF, 0xD9];

        [Fact]
        public void EnsureValid_EmptyFile_Throws()
        {
            var ex = Assert.Throws<DomainValidationException>(() =>
                ProfilePictureRules.EnsureValid("image/png", []));

            Assert.Contains("required", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void EnsureValid_TooBig_Throws()
        {
            var bytes = new byte[ProfilePictureRules.MaxBytes + 1];
            Array.Copy(PngBytes, bytes, PngBytes.Length);

            var ex = Assert.Throws<DomainValidationException>(() =>
                ProfilePictureRules.EnsureValid("image/png", bytes));

            Assert.Contains("larger", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void EnsureValid_ExactlyAtLimit_DoesNotThrow()
        {
            var bytes = new byte[ProfilePictureRules.MaxBytes];
            Array.Copy(PngBytes, bytes, PngBytes.Length);

            ProfilePictureRules.EnsureValid("image/png", bytes);
        }

        [Fact]
        public void EnsureValid_PngSignature_WithPngContentType_DoesNotThrow()
        {
            ProfilePictureRules.EnsureValid("image/png", PngBytes);
        }

        [Fact]
        public void EnsureValid_JpegSignature_WithJpegContentType_DoesNotThrow()
        {
            ProfilePictureRules.EnsureValid("image/jpeg", JpegBytes);
        }

        [Fact]
        public void EnsureValid_GifSignature_IsRejected()
        {
            var gif = Encoding.ASCII.GetBytes("GIF89a-picture");

            var ex = Assert.Throws<DomainValidationException>(() =>
                ProfilePictureRules.EnsureValid("image/gif", gif));

            Assert.Contains("PNG and JPEG", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void EnsureValid_TextFileRenamedPng_IsRejectedBySignature()
        {
            var text = Encoding.UTF8.GetBytes("this is not an image file");

            var ex = Assert.Throws<DomainValidationException>(() =>
                ProfilePictureRules.EnsureValid("image/png", text));

            Assert.Contains("PNG and JPEG", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void EnsureValid_JpegBytesDeclaredAsPng_ThrowsMismatch()
        {
            var ex = Assert.Throws<DomainValidationException>(() =>
                ProfilePictureRules.EnsureValid("image/png", JpegBytes));

            Assert.Contains("does not match", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void EnsureValid_JpegMissingEndMarker_IsRejected()
        {
            var spoofed = new byte[] { 0xFF, 0xD8, 0x41, 0x41, 0x41, 0x41 };

            var ex = Assert.Throws<DomainValidationException>(() =>
                ProfilePictureRules.EnsureValid("image/jpeg", spoofed));

            Assert.Contains("PNG and JPEG", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
    }
}
