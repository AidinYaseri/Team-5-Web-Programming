using System.ComponentModel.DataAnnotations;

namespace Animal.Client.Contracts
{
    public class UpdateUserRequest
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(254)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Phone]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        public static UpdateUserRequest FromResponse(UserResponse user) => new()
        {
            Name = user.Name,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber
        };
    }
}
