namespace Animal.Domain.Exceptions
{
    public class EntityConflictException : DomainException
    {
        public EntityConflictException(string message) : base(message)
        {
        }
    }
}