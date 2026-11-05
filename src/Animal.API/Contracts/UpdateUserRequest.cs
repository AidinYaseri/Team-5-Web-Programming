using System.ComponentModel.DataAnnotations;

namespace Animal.API.Contracts
{
    /// <summary>Profile fields to create or replace for the authenticated user.</summary>
    public sealed record UpdateUserRequest
    {
        [Required]
        [StringLength(100)]
        public string Name { get; init; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(254)]
        public string Email { get; init; } = string.Empty;

        [Required]
        [Phone]
        [StringLength(20)]
        public string PhoneNumber { get; init; } = string.Empty;
    }
}
