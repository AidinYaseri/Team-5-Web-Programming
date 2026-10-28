using Animal.Data;
using Animal.Data.Services;
using Animal.Domain.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// LocalDB by default (no password). Override with user-secrets or the ConnectionStrings__AnimalDb environment variable.
var connectionString = builder.Configuration.GetConnectionString("AnimalDb")
    ?? throw new InvalidOperationException("Connection string 'AnimalDb' is missing.");
builder.Services.AddAnimalData(connectionString);

// Factory on purpose: otherwise DI picks the IEnumerable<Pet> constructor and injects an empty list.
builder.Services.AddSingleton<IPetService>(_ => new InMemoryPetService());
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
