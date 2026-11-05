using Animal.Domain.Exceptions;
using Animal.Domain.Services;

namespace Animal.UnitTests.Domain
{
    public sealed class NotificationRulesTests
    {
        [Fact]
        public void ValidateMessage_TrimsTextAndAllowsMaximumLength()
        {
            var message = new string('x', NotificationRules.MaximumMessageLength);

            var result = NotificationRules.ValidateMessage($" {message} ");

            Assert.Equal(message, result);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void ValidateMessage_EmptyText_Throws(string message)
        {
            Assert.Throws<DomainValidationException>(() => NotificationRules.ValidateMessage(message));
        }

        [Fact]
        public void ValidateMessage_OverMaximumLength_Throws()
        {
            var message = new string('x', NotificationRules.MaximumMessageLength + 1);

            Assert.Throws<DomainValidationException>(() => NotificationRules.ValidateMessage(message));
        }
    }
}
