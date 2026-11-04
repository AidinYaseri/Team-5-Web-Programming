using System.ComponentModel.DataAnnotations;

namespace Animal.Client.Contracts
{
    // Body for POST /api/pets and PUT /api/pets/{id}. Also the form model, so the form shows the same
    // limits the API checks. The API still validates everything, this just gives quicker feedback.
    public class PetRequest
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

        public bool IsAvailable { get; set; } = true;

        [Range(1, int.MaxValue, ErrorMessage = "Pick a shelter.")]
        public int ShelterId { get; set; }

        public static PetRequest FromResponse(PetResponse pet) => new()
        {
            Name = pet.Name,
            Species = pet.Species,
            Breed = pet.Breed,
            Age = pet.Age,
            IsAvailable = pet.IsAvailable,
            ShelterId = pet.ShelterId
        };
    }
}
