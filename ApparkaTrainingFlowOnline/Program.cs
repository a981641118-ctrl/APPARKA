using ApparkaTrainingFlowOnline.Data;
using ApparkaTrainingFlowOnline.Services;
using ApparkaTrainingFlowOnline.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

var postgresConnection = builder.Configuration["POSTGRES_CONNECTION"]
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("No se configuró la conexión PostgreSQL. Usa ConnectionStrings:DefaultConnection o POSTGRES_CONNECTION.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(postgresConnection, npgsql => npgsql.EnableRetryOnFailure(3)));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "ApparkaTraining.Auth";
        options.ExpireTimeSpan = TimeSpan.FromHours(10);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole(AppRoles.Administrator));
    options.AddPolicy("HrOrAdmin", p => p.RequireRole(AppRoles.Hr, AppRoles.Administrator));
    options.AddPolicy("SupervisorOrAdmin", p => p.RequireRole(AppRoles.Supervisor, AppRoles.Administrator));
    options.AddPolicy("CollaboratorOnly", p => p.RequireRole(AppRoles.Collaborator));
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("sensitive", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<PeruClock>();
builder.Services.AddSingleton<ValidationCodeService>();
builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<TrainingScheduleService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<CurrentUserService>();
builder.Services.AddScoped<TrainingWorkflowService>();
builder.Services.AddScoped<InvitationEmailService>();
builder.Services.Configure<TrainingOptions>(builder.Configuration.GetSection("Training"));
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.Use(async (context, next) =>
{
    if (!context.Request.Cookies.ContainsKey("ApparkaDeviceId"))
    {
        context.Response.Cookies.Append("ApparkaDeviceId", Guid.NewGuid().ToString("N"), new CookieOptions
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddYears(1)
        });
    }
    await next();
});
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapGet("/health", () => Results.Ok(new { status = "ok", utc = DateTimeOffset.UtcNow })).AllowAnonymous();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await SeedData.InitializeAsync(scope.ServiceProvider);
}

app.Run();
