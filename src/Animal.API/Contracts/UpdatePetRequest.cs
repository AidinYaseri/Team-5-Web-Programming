using System.ComponentModel.DataAnnotations;

namespace Animal.API.Contracts
{
    // Body for PUT /api/pets/{id}. Replaces every field of the pet.
    public class UpdatePetRequest
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Species { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Breed { get; set; }

        [Range(0, 100)]
        public int Age { get; set; }

        public bool IsAvailable { get; set; }

        [Range(1, int.MaxValue)]
        public int ShelterId { get; set; }
    }
}
