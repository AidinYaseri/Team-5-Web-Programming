namespace Animal.API.Authorization;

public static class AuthorizationPolicies
{
    public const string PetsWrite = "pets.write";
    public const string SheltersWrite = "shelters.write";
    public const string UsersRead = "users.read";
    public const string UsersWrite = "users.write";
    public const string AdoptionsRead = "adoptions.read";
    public const string AdoptionsWrite = "adoptions.write";
    public const string AdoptionsReview = "adoptions.review";
    public const string AppointmentsRead = "appointments.read";
    public const string AppointmentsWrite = "appointments.write";
    public const string NotificationsRead = "notifications.read";
}
