namespace Animal.Domain.Exceptions
{
    // Base class for errors caused by breaking a business rule. The API maps these to 4xx responses.
    public abstract class DomainException : Exception
    {
        protected DomainException(string message) : base(message)
        {
        }
    }
}
