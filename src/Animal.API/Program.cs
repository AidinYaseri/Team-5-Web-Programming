using Animal.API.Auth;
using Animal.API.ErrorHandling;
using Animal.API.Services;
using Animal.Data;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// LocalDB by default (no password). Override with user-secrets or the ConnectionStrings__AnimalDb environment variable.
var connectionString = builder.Configuration.GetConnectionString("AnimalDb")
    ?? throw new InvalidOperationException("Connection string 'AnimalDb' is missing.");
builder.Services.AddAnimalData(connectionString);

// Application services. Scoped, like the DbContext they use through the repositories.
builder.Services.AddScoped<IPetService, PetService>();
builder.Services.AddScoped<IShelterService, ShelterService>();

builder.Services.AddAuthentication("Permission")
    .AddScheme<AuthenticationSchemeOptions, PermissionAuthenticationHandler>("Permission", _ => { });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("pets.write", policy =>
    {
        policy.RequireClaim("permission", "pets.write");
    });

    options.AddPolicy("shelters.write", policy =>
    {
        policy.RequireClaim("permission", "shelters.write");
    });
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
