using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using RankingCyY.Data;

var builder = WebApplication.CreateBuilder(args);

// 1) Definir la política de CORS que permita solo el origen Angular (http://localhost:4200)
const string AngularCorsPolicy = "PermitirAngular";

builder.Services.AddCors(options =>
{
    options.AddPolicy(AngularCorsPolicy, policy =>
    {
        policy
            .WithOrigins("http://localhost:4200") // Origen permitido
            .AllowAnyMethod()                      // GET, POST, PUT, DELETE, etc.
            .AllowAnyHeader()                      // Cualquier encabezado (Content-Type, Authorization...)
            .AllowCredentials();                   // Permitir envío de cookies/credenciales si las usas
    });
});

// Configurar la conexión a PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configuración de la autenticación basada en cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";   // Ruta de login
        options.LogoutPath = "/Auth/Logout"; // Ruta de logout
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // Duración de la cookie
        options.SlidingExpiration = true;    // Actualiza expiración en cada solicitud
    });

// Registrar servicios de la aplicación
builder.Services.AddHostedService<RankingCyY.Services.TemporadaService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 2) En Development, habilitar Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 3) Habilitar CORS justo antes de usar Authentication/Authorization
app.UseCors(AngularCorsPolicy);

app.UseHttpsRedirection();

app.UseAuthentication(); // Habilitar autenticación (cookies)
app.UseAuthorization();  // Habilitar autorización

app.MapControllers();

app.Run();
