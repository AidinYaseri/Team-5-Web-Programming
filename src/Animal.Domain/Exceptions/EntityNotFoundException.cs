namespace Animal.Domain.Exceptions
{
    public class EntityNotFoundException : DomainException
    {
        public EntityNotFoundException(string entityName, int id)
            : base($"{entityName} {id} was not found.")
        {
            EntityName = entityName;
            Id = id;
        }

        // For lookups that have no id to report, e.g. "the current user's profile".
        public EntityNotFoundException(string entityName, string message)
            : base(message)
        {
            EntityName = entityName;
        }

        public string EntityName { get; }
        public int? Id { get; }
    }
}
