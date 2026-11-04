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

        public string EntityName { get; }
        public int Id { get; }
    }
}
