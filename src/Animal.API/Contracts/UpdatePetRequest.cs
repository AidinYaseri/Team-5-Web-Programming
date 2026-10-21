using System.ComponentModel.DataAnnotations;
using Animal.Domain.Entities;

namespace Animal.API.Contracts
{
    // Body for PUT /api/pets/{id}. Replaces every field.
    public class UpdatePetRequest
    {
        [Required, StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string Species { get; set; } = string.Empty;

        [StringLength(50)]
        public string Breed { get; set; } = string.Empty;

        [Range(0, 100)]
        public int Age { get; set; }

        public bool IsAvailable { get; set; }

        [Range(1, int.MaxValue)]
        public int ShelterId { get; set; }

        public Pet ToPet(int id) => new()
        {
            Id = id,
            Name = Name.Trim(),
            Species = Species.Trim(),
            Breed = Breed.Trim(),
            Age = Age,
            IsAvailable = IsAvailable,
            ShelterId = ShelterId
        };
    }
}
