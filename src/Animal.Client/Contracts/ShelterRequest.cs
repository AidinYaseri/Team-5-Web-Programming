using System.ComponentModel.DataAnnotations;

namespace Animal.Client.Contracts
{
    public class ShelterRequest
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
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public static ShelterRequest FromResponse(ShelterResponse shelter) => new()
        {
            Name = shelter.Name,
            Address = shelter.Address,
            PhoneNumber = shelter.PhoneNumber,
            Email = shelter.Email
        };
    }
}