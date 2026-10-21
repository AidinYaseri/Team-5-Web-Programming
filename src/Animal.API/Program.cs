using System.Text.Json.Serialization;
using Animal.API.ErrorHandling;
using Animal.API.Services;
using Animal.Data.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Enums go over the wire as strings ("Pending", not 0), see docs/milestone-3/api-contracts.md.
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
// Singleton because the in-memory list is the "database" until WI-2 adds EF Core.
// Factory on purpose: otherwise DI picks the IEnumerable<Pet> constructor and injects an empty list.
builder.Services.AddSingleton<IPetRepository>(_ => new InMemoryPetRepository());
builder.Services.AddScoped<IPetService, PetService>();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
