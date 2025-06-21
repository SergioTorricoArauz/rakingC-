using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RankingCyY.Data;
using RankingCyY.Models;

namespace RankingCyY.Services
{
    public class TemporadaService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TemporadaService> _logger;
        private readonly TimeSpan _intervalo = TimeSpan.FromHours(1); // Intervalo de ejecución

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

                        // 2. Asignar insignias a los 3 primeros clientes de la temporada
                        await AsignarInsignias(temporadaActiva.Id, db);
                    }

                    // 2. Activar temporada pendiente si corresponde
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

        private async Task AsignarInsignias(int temporadaId, AppDbContext db)
        {
            var ranking = await db.Puntajes
                .Where(p => p.TemporadaId == temporadaId)  // Filtrar por la temporada
                .GroupBy(p => p.ClienteId)  // Agrupar por cliente
                .Select(g => new
                {
                    ClienteId = g.Key,  // Obtener el Id del Cliente
                    PuntosTotales = g.Sum(p => p.Puntos)  // Sumar los puntos por cliente
                })
                .OrderByDescending(r => r.PuntosTotales)  // Ordenar por los puntos totales
                .Take(3)  // Tomar solo los 3 primeros
                .ToListAsync();

            // Obtener las insignias de temporada
            var insignias = await db.Insignias
                .Where(i => i.Nombre.StartsWith("Temporada Top"))
                .ToListAsync();

            var insigniasOtorgadas = new List<string>();

            for (int i = 0; i < ranking.Count; i++)  // Solo asignamos a los primeros 3 clientes
            {
                var puntaje = ranking[i];
                Insignias? insignia = null;

                // Asignar la insignia correspondiente según el puesto en el ranking
                if (i == 0)
                {
                    insignia = insignias.FirstOrDefault(i => i.Nombre == "Temporada Top 1")!;
                }
                else if (i == 1)
                {
                    insignia = insignias.FirstOrDefault(i => i.Nombre == "Temporada Top 2")!;
                }
                else if (i == 2)
                {
                    insignia = insignias.FirstOrDefault(i => i.Nombre == "Temporada Top 3")!;
                }

                if (insignia != null)
                {
                    // Verificar si el cliente ya tiene esta insignia
                    var clienteInsigniaExistente = await db.ClienteInsignias
                        .AnyAsync(ci => ci.ClienteId == puntaje.ClienteId && ci.InsigniaId == insignia.Id);

                    if (!clienteInsigniaExistente)
                    {
                        // Asignar la insignia al cliente
                        db.ClienteInsignias.Add(new ClienteInsignia
                        {
                            ClienteId = puntaje.ClienteId,
                            InsigniaId = insignia.Id,
                            FechaOtorgada = DateTime.UtcNow.Date
                        });
                        insigniasOtorgadas.Add(insignia.Nombre);  // Guardar el nombre de la insignia otorgada
                    }
                }
            }

            // Guardar cambios en la base de datos
            await db.SaveChangesAsync();
            _logger.LogInformation("Insignias otorgadas: {Insignias}", string.Join(", ", insigniasOtorgadas));
        }
    }

}

