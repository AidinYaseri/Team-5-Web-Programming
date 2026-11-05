using System.ComponentModel.DataAnnotations;

namespace Animal.API.Contracts
{
    // Body for POST /api/pets. The Id is chosen by the database, so it isn't part of the request.
    public class CreatePetRequest
    {
        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Species { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Breed { get; set; }

        [Range(0, 100)]
        public int Age { get; set; }

        [Range(1, int.MaxValue)]
        public int ShelterId { get; set; }
    }
}
