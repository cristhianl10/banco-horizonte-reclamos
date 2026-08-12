var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
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
app.UseAuthorization();
app.MapControllers();

app.Run();
