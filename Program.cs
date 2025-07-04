// Program.cs  –  .NET 7/8 top-level statements
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using RankingCyY.Data;
using RankingCyY.Domain;    // ITemporadaDomainService / TemporadaDomainService
using RankingCyY.Services;  // TemporadaSchedulerService / SchedulerOptions / PuntosService
using RankingCyY.Hubs;      // HistoriaHub, CarritoHub

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
// 5) Servicios de negocio
// ────────────────────────────────────────────────────────────────────────────────
builder.Services.Configure<SchedulerOptions>(
    builder.Configuration.GetSection("Scheduler"));

builder.Services.AddScoped<ITemporadaDomainService, TemporadaDomainService>();
builder.Services.AddScoped<IPuntosService, PuntosService>(); // ⭐ Nuevo servicio
builder.Services.AddHostedService<TemporadaSchedulerService>();
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

// Mapear los Hubs de SignalR
app.MapHub<HistoriaHub>("/historiahub");
app.MapHub<CarritoHub>("/carritohub");

app.Run();
