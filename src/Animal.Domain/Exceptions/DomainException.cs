namespace Animal.Domain.Exceptions
{
    // Base type for errors raised by the domain, so the API can map them to HTTP status codes in one place.
    public abstract class DomainException : Exception
    {
        protected DomainException(string message) : base(message)
        {
        }
    }
}
