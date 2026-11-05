using Animal.API.Authorization;
using Animal.API.ErrorHandling;
using Animal.API.Services;
using Animal.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(TimeProvider.System);

// Only needed if a browser calls the API directly; the Blazor client calls it from its server.
var clientOrigin = builder.Configuration["Cors:ClientOrigin"];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    if (!string.IsNullOrWhiteSpace(clientOrigin))
    {
        policy.WithOrigins(clientOrigin).AllowAnyHeader().AllowAnyMethod();
    }
}));

// LocalDB in Development. Override with user-secrets or the ConnectionStrings__AnimalDb environment variable.
var connectionString = builder.Configuration.GetConnectionString("AnimalDb")
    ?? throw new InvalidOperationException("Connection string 'AnimalDb' is missing.");
builder.Services.AddAnimalData(connectionString);

// Application services. Scoped, like the DbContext they use through the repositories.
builder.Services.AddScoped<IPetService, PetService>();
builder.Services.AddScoped<IShelterService, ShelterService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IAdoptionService, AdoptionService>();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

// Resource server: validate the provider's access tokens (signature, issuer, audience, lifetime).
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Authentication:Authority"]
            ?? throw new InvalidOperationException("Authentication authority is missing.");
        options.Audience = builder.Configuration["Authentication:Audience"]
            ?? throw new InvalidOperationException("Authentication audience is missing.");
        // A local provider such as Keycloak may run on plain http in Development only.
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.MapInboundClaims = false;
        options.TokenValidationParameters.ValidateIssuerSigningKey = true;
        options.TokenValidationParameters.ValidateIssuer = true;
        options.TokenValidationParameters.ValidateAudience = true;
        options.TokenValidationParameters.ValidateLifetime = true;
    });

// Adds the local user id for the token's sub, so services can work with Users.Id.
builder.Services.AddTransient<IClaimsTransformation, SubjectToUserIdClaimsTransformation>();
builder.Services.AddAuthorization(AuthorizationPolicies.Configure);

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
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
