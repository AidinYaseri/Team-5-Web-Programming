namespace Animal.Domain.Entities
{
    public class User
    {
        public int Id { get; set; }
        // Stable identity-provider subject (OIDC sub); never assume it is the local numeric user ID.
        public string ExternalId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
    }
}
