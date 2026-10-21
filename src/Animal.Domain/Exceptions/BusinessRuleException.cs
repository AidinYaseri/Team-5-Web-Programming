namespace Animal.Domain.Exceptions
{
    // Thrown when a request is valid on its own but breaks a rule given the current state
    // (e.g. adopting a pet that isn't available). Maps to 409.
    public class BusinessRuleException : DomainException
    {
        public BusinessRuleException(string message) : base(message)
        {
        }
    }
}
