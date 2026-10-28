namespace Animal.Domain.Exceptions
{
    // Thrown when an entity breaks one or more business rules. Errors holds every broken rule, not just the first.
    public class DomainValidationException : DomainException
    {
        public DomainValidationException(IReadOnlyList<string> errors)
            : base("One or more business rules were broken: " + string.Join(" ", errors))
        {
            Errors = errors;
        }

        public DomainValidationException(string error) : this([error])
        {
        }

        public IReadOnlyList<string> Errors { get; }
    }
}
