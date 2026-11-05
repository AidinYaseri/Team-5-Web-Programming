using System.ComponentModel.DataAnnotations;

namespace Animal.API.Contracts
{
    public class CreateShelterRequest
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Address { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(254)]
        public string Email { get; set; } = string.Empty;
    }
}