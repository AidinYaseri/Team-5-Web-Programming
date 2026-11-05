using Microsoft.AspNetCore.Authorization;

namespace Animal.API.Authorization
{
    // One named policy per scope in docs/milestone-3/api-contracts.md. The policy name is the scope string.
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

        // Not a scope: GET /api/adoptions/{id} is allowed with adoptions.read (own) or adoptions.review (any).
        public const string AdoptionsReadOrReview = "adoptions.read-or-review";

        public static readonly string[] All =
        [
            PetsWrite, SheltersWrite, UsersRead, UsersWrite, AdoptionsRead, AdoptionsWrite,
            AdoptionsReview, AppointmentsRead, AppointmentsWrite, NotificationsRead
        ];

        // These work on the caller's own rows, so the token's sub must already map to a local user
        // (see SubjectToUserIdClaimsTransformation). Users policies are left out: PUT /api/users/me creates that user.
        // adoptions.review is left out too: reviewers act on other people's applications.
        private static readonly HashSet<string> NeedsLocalUser =
            [AdoptionsRead, AdoptionsWrite, AppointmentsRead, AppointmentsWrite, NotificationsRead];

        public static void Configure(AuthorizationOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            // Deny by default: an endpoint without [Authorize] or [AllowAnonymous] still needs a signed-in user.
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            foreach (var scope in All)
            {
                options.AddPolicy(scope, policy =>
                {
                    policy.RequireAuthenticatedUser()
                        .RequireAssertion(context => ScopeClaims.HasScope(context.User, scope));

                    if (NeedsLocalUser.Contains(scope))
                    {
                        policy.RequireAssertion(context => ScopeClaims.HasMappedUser(context.User));
                    }
                });
            }

            options.AddPolicy(AdoptionsReadOrReview, policy => policy
                .RequireAuthenticatedUser()
                .RequireAssertion(context =>
                    ScopeClaims.HasScope(context.User, AdoptionsReview)
                    || (ScopeClaims.HasScope(context.User, AdoptionsRead) && ScopeClaims.HasMappedUser(context.User))));
        }
    }
}
