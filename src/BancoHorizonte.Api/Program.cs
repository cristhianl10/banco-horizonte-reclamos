using BancoHorizonte.Api.Application;
using BancoHorizonte.Api.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

if (int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
var connectionString = builder.Configuration.GetConnectionString("Supabase")
    ?? throw new InvalidOperationException("Configura ConnectionStrings:Supabase mediante user-secrets o variables de entorno.");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("ConnectionStrings:Supabase no puede estar vacía.");

if (args.Contains("--apply-requirements-database", StringComparer.OrdinalIgnoreCase))
{
    var databaseDirectory = Path.Combine(builder.Environment.ContentRootPath, "database");
    if (!Directory.Exists(databaseDirectory))
        databaseDirectory = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "..", "database"));
    if (!Directory.Exists(databaseDirectory))
        throw new DirectoryNotFoundException("No se encontró el directorio database del repositorio.");
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    foreach (var script in new[] { "migrations/001_align_finresolve_requirements.sql", "demo-data.sql" })
    {
        var sql = await File.ReadAllTextAsync(Path.Combine(databaseDirectory, script));
        await using var command = new NpgsqlCommand(sql, connection) { CommandTimeout = 120 };
        await command.ExecuteNonQueryAsync();
        Console.WriteLine($"Aplicado: {script}");
    }
    await using var verification = new NpgsqlCommand(
        "select count(*) from public.reclamos where codigo like 'BH-DEMO-%'", connection);
    Console.WriteLine($"Reclamos demo disponibles: {await verification.ExecuteScalarAsync()}");
    return;
}
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IPriorityAndSlaService, PriorityAndSlaService>();
builder.Services.AddScoped<ComplaintService>();
builder.Services.AddScoped<IClaimsTransformation, DatabaseRoleClaimsTransformation>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    var supabaseUrl = builder.Configuration["Supabase:Url"]?.TrimEnd('/');
    options.Authority = string.IsNullOrWhiteSpace(supabaseUrl) ? "https://placeholder.supabase.co/auth/v1" : $"{supabaseUrl}/auth/v1";
    options.Audience = "authenticated";
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.TokenValidationParameters.NameClaimType = "email";
    options.TokenValidationParameters.RoleClaimType = System.Security.Claims.ClaimTypes.Role;
});
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    options.AddPolicy("registration-check", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});
var allowedOrigins = (builder.Configuration["Cors:AllowedOrigins"]
        ?? builder.Configuration["Cors:AngularOrigin"]
        ?? "http://localhost:4200")
    .Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();
builder.Services.AddCors(options =>
{
    options.AddPolicy("angular-client", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseCors("angular-client");
app.UseExceptionHandler();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
