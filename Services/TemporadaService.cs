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

                    // Verificar la temporada activa que haya terminado
                    var temporadaActiva = await db.Temporadas
                        .FirstOrDefaultAsync(t => t.EstaDisponible && t.Fin.Date <= hoy, stoppingToken);

                    if (temporadaActiva != null)
                    {
                        // Desactivar la temporada
                        temporadaActiva.EstaDisponible = false;

                        // Guardar cambios en la base de datos
                        await db.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation("Temporada finalizada automáticamente: {Id} - {Nombre}", temporadaActiva.Id, temporadaActiva.Nombre);
                    }
                }
                // Esperar el intervalo configurado antes de realizar la siguiente verificación
                await Task.Delay(_intervalo, stoppingToken);
            }
        }
    }
}

