namespace Animal.Domain.ValueObjects
{
    // Optional filters for searching pets. A null or empty value means "don't filter on this".
    public class PetSearchCriteria
    {
        public string? Species { get; set; }
        public int? MaxAge { get; set; }
        public bool AvailableOnly { get; set; }
    }
}
