using System.ComponentModel.DataAnnotations;

namespace Animal.API.Contracts
{
    // Query string for GET /api/pets. Every filter is optional.
    public class PetSearchQuery
    {
        [StringLength(50)]
        public string? Species { get; set; }

        [Range(0, 100)]
        public int? MaxAge { get; set; }

        public bool AvailableOnly { get; set; }
    }
}
