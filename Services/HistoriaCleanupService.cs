using Microsoft.EntityFrameworkCore;
using RankingCyY.Data;

namespace RankingCyY.Services
{
    public class HistoriaCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<HistoriaCleanupService> _logger;

        public HistoriaCleanupService(IServiceProvider serviceProvider, ILogger<HistoriaCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    await LimpiarHistoriasExpiradas(context);
                    
                    // Ejecutar cada hora
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en servicio de limpieza de historias");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }
        }

        private async Task LimpiarHistoriasExpiradas(AppDbContext context)
        {
            var ahora = DateTime.UtcNow;
            var historiasExpiradas = await context.Historias
                .Where(h => h.FechaExpiracion <= ahora && h.EstaActiva)
                .ToListAsync();

            foreach (var historia in historiasExpiradas)
            {
                historia.EstaActiva = false;
            }

            if (historiasExpiradas.Any())
            {
                await context.SaveChangesAsync();
                _logger.LogInformation($"Se desactivaron {historiasExpiradas.Count} historias expiradas");
            }
        }
    }
}