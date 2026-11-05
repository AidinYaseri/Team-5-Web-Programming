using Animal.Domain.Entities;

namespace Animal.API.Contracts
{
    /// <summary>Represents the authenticated user's profile.</summary>
    public sealed record UserResponse(int Id, string Name, string Email, string PhoneNumber)
    {
        public static UserResponse FromUser(User user) =>
            new(user.Id, user.Name, user.Email, user.PhoneNumber);
    }
}
