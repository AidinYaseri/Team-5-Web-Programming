namespace Animal.API.Services
{
    // The signed-in user, read only from validated token claims. Never from a body, header or route.
    public interface ICurrentUser
    {
        // The identity provider's subject (token "sub"). Used to find or create the profile (WI-8).
        string Subject { get; }

        // The local Users.Id that the subject maps to. Only available once the user has a profile.
        int UserId { get; }
    }
}
