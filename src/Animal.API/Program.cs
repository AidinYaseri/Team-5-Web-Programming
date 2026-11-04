using Animal.API.ErrorHandling;
using Animal.API.Authorization;
using Animal.API.Services;
using Animal.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

// LocalDB by default (no password). Override with user-secrets or the ConnectionStrings__AnimalDb environment variable.
var connectionString = builder.Configuration.GetConnectionString("AnimalDb")
    ?? throw new InvalidOperationException("Connection string 'AnimalDb' is missing.");
builder.Services.AddAnimalData(connectionString);

// Application services. Scoped, like the DbContext they use through the repositories.
builder.Services.AddScoped<IPetService, PetService>();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Authentication:Authority"]
            ?? throw new InvalidOperationException("Authentication authority is missing.");
        options.Audience = builder.Configuration["Authentication:Audience"]
            ?? throw new InvalidOperationException("Authentication audience is missing.");
        options.MapInboundClaims = false;
        options.TokenValidationParameters.ValidateIssuerSigningKey = true;
        options.TokenValidationParameters.ValidateIssuer = true;
        options.TokenValidationParameters.ValidateAudience = true;
        options.TokenValidationParameters.ValidateLifetime = true;
    });
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.AddPolicy(AuthorizationPolicies.PetsWrite, policy =>
        policy.RequireAuthenticatedUser()
            .RequireAssertion(context => ScopeClaims.HasScope(context.User, AuthorizationPolicies.PetsWrite)));
    options.AddPolicy(AuthorizationPolicies.SheltersWrite, policy =>
        policy.RequireAuthenticatedUser()
            .RequireAssertion(context => ScopeClaims.HasScope(context.User, AuthorizationPolicies.SheltersWrite)));
    options.AddPolicy(AuthorizationPolicies.UsersRead, policy =>
        policy.RequireAuthenticatedUser()
            .RequireAssertion(context => ScopeClaims.HasScope(context.User, AuthorizationPolicies.UsersRead)));
    options.AddPolicy(AuthorizationPolicies.UsersWrite, policy =>
        policy.RequireAuthenticatedUser()
            .RequireAssertion(context => ScopeClaims.HasScope(context.User, AuthorizationPolicies.UsersWrite)));
    options.AddPolicy(AuthorizationPolicies.AdoptionsRead, policy =>
        policy.RequireAuthenticatedUser()
            .RequireAssertion(context => ScopeClaims.HasScope(context.User, AuthorizationPolicies.AdoptionsRead)));
    options.AddPolicy(AuthorizationPolicies.AdoptionsWrite, policy =>
        policy.RequireAuthenticatedUser()
            .RequireAssertion(context => ScopeClaims.HasScope(context.User, AuthorizationPolicies.AdoptionsWrite)));
    options.AddPolicy(AuthorizationPolicies.AdoptionsReview, policy =>
        policy.RequireAuthenticatedUser()
            .RequireAssertion(context => ScopeClaims.HasScope(context.User, AuthorizationPolicies.AdoptionsReview)));
    options.AddPolicy(AuthorizationPolicies.AppointmentsRead, policy =>
        policy.RequireAuthenticatedUser()
            .RequireAssertion(context => ScopeClaims.HasScope(context.User, AuthorizationPolicies.AppointmentsRead)));
    options.AddPolicy(AuthorizationPolicies.AppointmentsWrite, policy =>
        policy.RequireAuthenticatedUser()
            .RequireAssertion(context => ScopeClaims.HasScope(context.User, AuthorizationPolicies.AppointmentsWrite)));
    options.AddPolicy(AuthorizationPolicies.NotificationsRead, policy =>
        policy.RequireAuthenticatedUser()
            .RequireAssertion(context => ScopeClaims.HasScope(context.User, AuthorizationPolicies.NotificationsRead)));
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
