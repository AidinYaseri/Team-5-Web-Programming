using Animal.API.ErrorHandling;
using Animal.API.Services;
using Animal.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
var clientOrigin = builder.Configuration["Cors:ClientOrigin"];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    if (!string.IsNullOrWhiteSpace(clientOrigin))
    {
        policy.WithOrigins(clientOrigin).AllowAnyHeader().AllowAnyMethod();
    }
}));
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Authentication:Authority"];
        options.Audience = builder.Configuration["Authentication:Audience"];
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.MapInboundClaims = false;
    });
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("notifications.read", policy => policy.RequireAssertion(context => HasUserId(context.User) && HasScope(context.User, "notifications.read")))
    .AddPolicy("notifications.write", policy => policy.RequireAssertion(context => HasUserId(context.User) && HasScope(context.User, "notifications.write")))
    .AddPolicy("appointments.read", policy => policy.RequireAssertion(context => HasUserId(context.User) && HasScope(context.User, "appointments.read")))
    .AddPolicy("appointments.write", policy => policy.RequireAssertion(context => HasUserId(context.User) && HasScope(context.User, "appointments.write")));

// LocalDB by default (no password). Override with user-secrets or the ConnectionStrings__AnimalDb environment variable.
var connectionString = builder.Configuration.GetConnectionString("AnimalDb")
    ?? throw new InvalidOperationException("Connection string 'AnimalDb' is missing.");
builder.Services.AddAnimalData(connectionString);

// Application services. Scoped, like the DbContext they use through the repositories.
builder.Services.AddScoped<IPetService, PetService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

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
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static bool HasScope(System.Security.Claims.ClaimsPrincipal user, string requiredScope) =>
    user.FindAll("scope").Concat(user.FindAll("scp"))
        .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        .Contains(requiredScope, StringComparer.Ordinal);

static bool HasUserId(System.Security.Claims.ClaimsPrincipal user) =>
    int.TryParse(user.FindFirst("user_id")?.Value, out var userId) && userId > 0;
