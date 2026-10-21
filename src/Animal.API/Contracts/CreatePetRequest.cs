using System.ComponentModel.DataAnnotations;
using Animal.Domain.Entities;

namespace Animal.API.Contracts
{
    // Body for POST /api/pets. New pets are always available.
    public class CreatePetRequest
    {
        [Required, StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string Species { get; set; } = string.Empty;

        [StringLength(50)]
        public string Breed { get; set; } = string.Empty;

        [Range(0, 100)]
        public int Age { get; set; }

        [Range(1, int.MaxValue)]
        public int ShelterId { get; set; }

        public Pet ToPet() => new()
        {
            Name = Name.Trim(),
            Species = Species.Trim(),
            Breed = Breed.Trim(),
            Age = Age,
            IsAvailable = true,
            ShelterId = ShelterId
        };
    }
}
