using RankingCyY.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace RankingCyY.Services
{
    public class TemporadaService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TemporadaService> _logger;
        private readonly TimeSpan _intervalo = TimeSpan.FromMinutes(30); // Puedes ajustar el intervalo

        public TemporadaService(IServiceProvider serviceProvider, ILogger<TemporadaService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var hoy = DateTime.UtcNow.Date;

                    _logger.LogInformation("TemporadaService ejecutando verificación a las {Fecha}", hoy);

                    // 1. Desactivar temporada activa si ya terminó
                    var temporadaActiva = await db.Temporadas
                        .FirstOrDefaultAsync(t => t.EstaDisponible && t.Fin.Date <= hoy, stoppingToken);

                    if (temporadaActiva != null)
                    {
                        temporadaActiva.EstaDisponible = false;
                        await db.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation("Temporada finalizada automáticamente: {Id} - {Nombre}", temporadaActiva.Id, temporadaActiva.Nombre);
                    }

                    // 2. Activar temporada pendiente si corresponde (no hay activa y la fecha de inicio ya llegó)
                    var hayActiva = await db.Temporadas.AnyAsync(t => t.EstaDisponible, stoppingToken);
                    if (!hayActiva)
                    {
                        var temporadaPendiente = await db.Temporadas
                            .Where(t => !t.EstaDisponible && t.Inicio <= hoy)
                            .OrderBy(t => t.Inicio)
                            .FirstOrDefaultAsync(stoppingToken);

                        if (temporadaPendiente != null)
                        {
                            temporadaPendiente.EstaDisponible = true;
                            await db.SaveChangesAsync(stoppingToken);
                            _logger.LogInformation("Temporada activada automáticamente: {Id} - {Nombre}", temporadaPendiente.Id, temporadaPendiente.Nombre);
                        }
                    }
                }
                await Task.Delay(_intervalo, stoppingToken);
            }
        }
    }
}

