using BancoHorizonte.Api.Application;
using BancoHorizonte.Api.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
var connectionString = builder.Configuration.GetConnectionString("Supabase")
    ?? throw new InvalidOperationException("Configura ConnectionStrings:Supabase mediante user-secrets o variables de entorno.");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("ConnectionStrings:Supabase no puede estar vacía.");
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
builder.Services.AddCors(options =>
{
    options.AddPolicy("angular-client", policy =>
        policy.WithOrigins(builder.Configuration["Cors:AngularOrigin"] ?? "http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("angular-client");
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
