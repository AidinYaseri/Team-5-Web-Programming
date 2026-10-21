namespace Animal.Domain.Exceptions
{
    // Thrown when an id doesn't match anything. Maps to 404.
    public class EntityNotFoundException : DomainException
    {
        public EntityNotFoundException(string entityName, int id)
            : base($"{entityName} {id} was not found.")
        {
            EntityName = entityName;
            Id = id;
        }

        public string EntityName { get; }
        public int Id { get; }
    }
}
