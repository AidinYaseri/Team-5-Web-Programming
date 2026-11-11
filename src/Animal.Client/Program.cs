using Animal.Client.Components;
using Animal.Client.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();

var authority = builder.Configuration["Authentication:Authority"];
var clientId = builder.Configuration["Authentication:ClientId"];
var identityConfigured = !string.IsNullOrWhiteSpace(authority) && !string.IsNullOrWhiteSpace(clientId);
var authentication = builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    if (identityConfigured)
    {
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    }
}).AddCookie(options =>
{
    // The cookie holds the tokens (SaveTokens = true), encrypted with Data Protection and never readable by scripts.
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    // No sliding: the session can't outlive the access token it carries (decision D5). Signing in again is
    // silent while the Keycloak session is still active.
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = false;
    options.Events.OnValidatePrincipal = context => AuthCookieEvents.RejectIfAccessTokenExpired(
        context, context.HttpContext.RequestServices.GetRequiredService<TimeProvider>());
});

if (identityConfigured)
{
    authentication.AddOpenIdConnect(options =>
    {
        options.Authority = authority;
        options.ClientId = clientId;
        // The local Keycloak runs on plain http, which is only allowed in Development (same rule as the API).
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        // Keep this value in user-secrets or an environment variable; it is only used server-side.
        options.ClientSecret = builder.Configuration["Authentication:ClientSecret"];
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.UsePkce = true;
        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = true;
        options.Scope.Clear();
        foreach (var scope in (builder.Configuration["Authentication:Scopes"] ??
                     "openid profile pets.write shelters.write users.read users.write adoptions.read adoptions.write adoptions.review appointments.read appointments.write notifications.read")
                     .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            options.Scope.Add(scope);
        }
    });
}

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<AccessTokenProvider>();
builder.Services.AddScoped<NotificationCountState>();
builder.Services.AddTransient<AccessTokenHandler>();

var apiBaseUrl = builder.Configuration["AnimalApi:BaseUrl"]
    ?? throw new InvalidOperationException("AnimalApi:BaseUrl is missing from configuration.");
// Every API client attaches the signed-in user's access token (AccessTokenHandler). Public pages still work signed out.
builder.Services.AddHttpClient<PetsApiClient>(client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AccessTokenHandler>();
builder.Services.AddHttpClient<SheltersApiClient>(client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AccessTokenHandler>();
builder.Services.AddHttpClient<UsersApiClient>(client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AccessTokenHandler>();
builder.Services.AddHttpClient<NotificationsApiClient>(client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AccessTokenHandler>();
builder.Services.AddHttpClient<AppointmentsApiClient>(client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AccessTokenHandler>();
builder.Services.AddHttpClient<AdoptionsApiClient>(client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AccessTokenHandler>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapGet("/auth/login", async (HttpContext context) =>
{
    if (!identityConfigured)
    {
        return Results.Problem("Set Authentication:Authority and Authentication:ClientId to enable sign-in.", statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties { RedirectUri = "/pets" });
    return Results.Empty;
}).AllowAnonymous();

app.MapPost("/auth/logout", async (HttpContext context) =>
{
    await context.RequestServices.GetRequiredService<Microsoft.AspNetCore.Antiforgery.IAntiforgery>()
        .ValidateRequestAsync(context);
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    if (identityConfigured)
    {
        await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties { RedirectUri = "/" });
    }
    else
    {
        context.Response.Redirect("/");
    }
}).AllowAnonymous();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();
