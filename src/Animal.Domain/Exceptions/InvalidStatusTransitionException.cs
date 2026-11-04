namespace Animal.Domain.Exceptions
{
    // Thrown when something is asked to move to a status it can't reach from its current one (e.g. approving a rejected application).
    public class InvalidStatusTransitionException : DomainException
    {
        public InvalidStatusTransitionException(string message) : base(message)
        {
        }
    }
}
