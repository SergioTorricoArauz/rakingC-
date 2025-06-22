using Microsoft.EntityFrameworkCore;
using RankingCyY.Data;

namespace RankingCyY.Services
{
    public class TemporadaStartupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TemporadaStartupService> _logger;
        private readonly TimeSpan _intervalo = TimeSpan.FromHours(1); // Ejecutar cada hora

        public TemporadaStartupService(IServiceProvider serviceProvider, ILogger<TemporadaStartupService> logger)
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

                    // Log para verificar la ejecución
                    _logger.LogInformation("TemporadaStartupService ejecutando verificación a las {Fecha}", hoy);

                    // Verificar si hay temporadas pendientes para activar
                    var temporadaPendiente = await db.Temporadas
                        .Where(t => !t.EstaDisponible && t.Inicio.Date <= hoy)
                        .OrderBy(t => t.Inicio)
                        .FirstOrDefaultAsync(stoppingToken);

                    if (temporadaPendiente != null)
                    {
                        // Activar la temporada
                        temporadaPendiente.EstaDisponible = true;
                        await db.SaveChangesAsync(stoppingToken);

                        // Log para verificar que la temporada se haya activado
                        _logger.LogInformation("Temporada activada automáticamente: {Id} - {Nombre}", temporadaPendiente.Id, temporadaPendiente.Nombre);
                    }
                }

                // Esperar el siguiente ciclo
                await Task.Delay(_intervalo, stoppingToken);
            }
        }
    }

}
