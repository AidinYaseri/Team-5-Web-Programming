using Animal.Data.Services;
using Animal.Domain.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
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
