using Microsoft.AspNetCore.Authorization;

namespace Animal.API.Authorization
{
    public static class AuthorizationPolicies
    {
        public const string NotificationsRead = "notifications.read";
        public const string AppointmentsRead = "appointments.read";
        public const string AppointmentsWrite = "appointments.write";

        public static void Configure(AuthorizationOptions options)
        {
            AddUserScopePolicy(options, NotificationsRead, NotificationsRead);
            AddUserScopePolicy(options, AppointmentsRead, AppointmentsRead);
            AddUserScopePolicy(options, AppointmentsWrite, AppointmentsWrite);
        }

        private static void AddUserScopePolicy(AuthorizationOptions options, string policyName, string scope) =>
            options.AddPolicy(policyName, policy => policy
                .RequireAuthenticatedUser()
                .RequireAssertion(context => ScopeClaims.HasMappedUser(context.User) && ScopeClaims.HasScope(context.User, scope)));
    }
}
