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
}).AddCookie();

if (identityConfigured)
{
    authentication.AddOpenIdConnect(options =>
    {
        options.Authority = authority;
        options.ClientId = clientId;
        // Keep this value in user-secrets or an environment variable; it is only used server-side.
        options.ClientSecret = builder.Configuration["Authentication:ClientSecret"];
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.UsePkce = true;
        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = true;
        options.Scope.Clear();
        foreach (var scope in (builder.Configuration["Authentication:Scopes"] ??
                     "openid profile pets.write shelters.write users.read users.write adoptions.read adoptions.write appointments.read appointments.write notifications.read")
                     .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            options.Scope.Add(scope);
        }
    });
}

builder.Services.AddAuthorization();
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

// The access token is returned only to the same-origin signed-in browser session. No client secret is sent here.
app.MapGet("/auth/access-token", async (HttpContext context) =>
{
    context.Response.Headers.CacheControl = "no-store, no-cache";
    context.Response.Headers.Pragma = "no-cache";
    if (context.User.Identity?.IsAuthenticated != true)
    {
        return Results.Unauthorized();
    }

    var token = await context.GetTokenAsync("access_token");
    return token is null ? Results.Unauthorized() : Results.Ok(new { accessToken = token });
}).AllowAnonymous();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();
