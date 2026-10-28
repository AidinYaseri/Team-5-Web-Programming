using Animal.Client.Components;
using Animal.Client.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// The client only talks to Animal.API over HTTP. Change AnimalApi:BaseUrl if the API runs somewhere else.
var apiBaseUrl = builder.Configuration["AnimalApi:BaseUrl"]
    ?? throw new InvalidOperationException("AnimalApi:BaseUrl is missing from configuration.");
builder.Services.AddHttpClient<PetsApiClient>(client => client.BaseAddress = new Uri(apiBaseUrl));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
