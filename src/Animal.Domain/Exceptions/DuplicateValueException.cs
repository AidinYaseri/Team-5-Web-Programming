namespace Animal.Domain.Exceptions
{
    public class DuplicateValueException : DomainException
    {
        public DuplicateValueException(string propertyName)
            : base($"A value for {propertyName} is already in use.")
        {
            PropertyName = propertyName;
        }

        public string PropertyName { get; }
    }
}
