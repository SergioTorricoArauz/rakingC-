// Program.cs  –  .NET 7/8 top-level statements
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using RankingCyY.Data;
using RankingCyY.Domain;    // ITemporadaDomainService / TemporadaDomainService
using RankingCyY.Services;  // TemporadaSchedulerService / SchedulerOptions
using RankingCyY.Hubs;      // HistoriaHub

var builder = WebApplication.CreateBuilder(args);

// ────────────────────────────────────────────────────────────────────────────────
// 1) CORS – solo permite solicitudes del front Angular en http://localhost:4200
// ────────────────────────────────────────────────────────────────────────────────
const string AngularCorsPolicy = "PermitirAngular";

builder.Services.AddCors(options =>
{
    options.AddPolicy(AngularCorsPolicy, policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// ────────────────────────────────────────────────────────────────────────────────
// 2) Base de datos PostgreSQL
// ────────────────────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ────────────────────────────────────────────────────────────────────────────────
// 3) Autenticación basada en cookies
// ────────────────────────────────────────────────────────────────────────────────
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
    });

// ────────────────────────────────────────────────────────────────────────────────
// 4) SignalR para tiempo real
// ────────────────────────────────────────────────────────────────────────────────
builder.Services.AddSignalR();

// ────────────────────────────────────────────────────────────────────────────────
// 5) Scheduler y dominio de temporadas
// ────────────────────────────────────────────────────────────────────────────────
// (En appsettings.json puedes sobreescribir el intervalo, por ejemplo:
//  "Scheduler": { "IntervalMinutes": 60 })
builder.Services.Configure<SchedulerOptions>(
    builder.Configuration.GetSection("Scheduler"));

builder.Services.AddScoped<ITemporadaDomainService, TemporadaDomainService>();
builder.Services.AddHostedService<TemporadaSchedulerService>();

// Servicio para limpiar historias expiradas
builder.Services.AddHostedService<HistoriaCleanupService>();

// ────────────────────────────────────────────────────────────────────────────────
// 6) API Controllers & Swagger
// ────────────────────────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ────────────────────────────────────────────────────────────────────────────────
// 7) Middleware pipeline
// ────────────────────────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(AngularCorsPolicy);

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Mapear el Hub de SignalR
app.MapHub<HistoriaHub>("/historiahub");

app.Run();
