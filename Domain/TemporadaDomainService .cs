using Microsoft.EntityFrameworkCore;
using RankingCyY.Data;
using RankingCyY.Models;

namespace RankingCyY.Domain
{
    public class TemporadaDomainService : ITemporadaDomainService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<TemporadaDomainService> _log;

        public TemporadaDomainService(AppDbContext db, ILogger<TemporadaDomainService> log)
        {
            _db = db;
            _log = log;
        }

        public async Task AsignarInsigniasTemporadaActivaAsync(CancellationToken ct)
        {
            var temporada = await _db.Temporadas.FirstOrDefaultAsync(t => t.EstaDisponible, ct);
            if (temporada is null) return;

            var ranking = await _db.Puntajes
                .Where(p => p.TemporadaId == temporada.Id && p.Puntos > 0) 
                .GroupBy(p => p.ClienteId)
                .Select(g => new { ClienteId = g.Key, Puntos = g.Sum(x => x.Puntos) })
                .Where(r => r.Puntos > 0)
                .OrderByDescending(r => r.Puntos)
                .Take(3)
                .ToListAsync(ct);

            if (ranking.Count == 0) return;

            var clienteIds = ranking.Select(r => r.ClienteId).ToList();
            var clientesExistentes = await _db.Clientes
                .Where(c => clienteIds.Contains(c.Id))
                .Select(c => c.Id)
                .ToListAsync(ct);

            if (clientesExistentes.Count != clienteIds.Count)
            {
                var faltantes = clienteIds.Except(clientesExistentes);
                _log.LogWarning("Algunos clientes del ranking no existen: {ClientesFaltantes}", string.Join(", ", faltantes));
            }

            var insignias = await _db.Insignias
                .Where(i => i.Nombre.StartsWith("Temporada Top"))
                .ToListAsync(ct);

            for (int i = 0; i < ranking.Count; i++)
            {
                var cliente = ranking[i];
                var nombre = $"Temporada Top {i + 1}";
                var insignia = insignias.FirstOrDefault(x => x.Nombre == nombre);
                if (insignia is null) continue;

                var yaTiene = await _db.ClienteInsignias
                    .AnyAsync(ci => ci.ClienteId == cliente.ClienteId && ci.InsigniaId == insignia.Id, ct);
                if (yaTiene) continue;

                _db.ClienteInsignias.Add(new ClienteInsignia
                {
                    ClienteId = cliente.ClienteId,
                    InsigniaId = insignia.Id,
                    FechaOtorgada = DateTime.UtcNow.Date
                });
            }

            await _db.SaveChangesAsync(ct);
            _log.LogInformation("Insignias de temporada {TempId} asignadas", temporada.Id);
        }

        public async Task AsignarInsigniasTemporadaFinalizadaAsync(int temporadaId, CancellationToken ct)
        {
            var temporada = await _db.Temporadas.FindAsync(temporadaId);
            if (temporada is null)
            {
                _log.LogWarning("Temporada con ID {TemporadaId} no encontrada", temporadaId);
                return;
            }

            var ranking = await _db.Puntajes
                .Where(p => p.TemporadaId == temporadaId && p.Puntos > 0)
                .GroupBy(p => p.ClienteId)
                .Select(g => new { ClienteId = g.Key, Puntos = g.Sum(x => x.Puntos) })
                .Where(r => r.Puntos > 0)
                .OrderByDescending(r => r.Puntos)
                .Take(3)
                .ToListAsync(ct);

            if (ranking.Count == 0)
            {
                _log.LogInformation("No hay puntajes para asignar insignias en la temporada {TemporadaId}", temporadaId);
                return;
            }

            var clienteIds = ranking.Select(r => r.ClienteId).ToList();
            var clientesExistentes = await _db.Clientes
                .Where(c => clienteIds.Contains(c.Id))
                .Select(c => c.Id)
                .ToListAsync(ct);

            if (clientesExistentes.Count != clienteIds.Count)
            {
                var faltantes = clienteIds.Except(clientesExistentes);
                _log.LogWarning("Algunos clientes del ranking no existen: {ClientesFaltantes}", string.Join(", ", faltantes));
            }

            var insignias = await _db.Insignias
                .Where(i => i.Nombre.StartsWith("Temporada Top"))
                .ToListAsync(ct);

            var insigniasYaAsignadas = await _db.ClienteInsignias
                .Include(ci => ci.Insignia)
                .Where(ci => ci.Insignia.Nombre.StartsWith("Temporada Top"))
                .AnyAsync(ct);

            if (insigniasYaAsignadas)
            {
                _log.LogInformation("Ya se han asignado insignias de temporada finalizada previamente para la temporada {TemporadaId}", temporadaId);
            }

            int insigniasAsignadas = 0;

            using var transaction = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                for (int i = 0; i < ranking.Count; i++)
                {
                    var cliente = ranking[i];
                    var nombre = $"Temporada Top {i + 1}";
                    var insignia = insignias.FirstOrDefault(x => x.Nombre == nombre);
                    if (insignia is null)
                    {
                        _log.LogWarning("Insignia {Nombre} no encontrada", nombre);
                        continue;
                    }

                    var yaTiene = await _db.ClienteInsignias
                        .AnyAsync(ci => ci.ClienteId == cliente.ClienteId && ci.InsigniaId == insignia.Id, ct);
                    if (yaTiene)
                    {
                        _log.LogInformation("Cliente {ClienteId} ya tiene la insignia {InsigniaId}", cliente.ClienteId, insignia.Id);
                        continue;
                    }

                    _db.ClienteInsignias.Add(new ClienteInsignia
                    {
                        ClienteId = cliente.ClienteId,
                        InsigniaId = insignia.Id,
                        FechaOtorgada = DateTime.UtcNow
                    });

                    insigniasAsignadas++;
                    _log.LogInformation("Asignada insignia {Nombre} al cliente {ClienteId} por temporada {TemporadaId}", nombre, cliente.ClienteId, temporadaId);
                }

                if (insigniasAsignadas > 0)
                {
                    await _db.SaveChangesAsync(ct);
                    await transaction.CommitAsync(ct);
                    _log.LogInformation("Se asignaron {Cantidad} insignias para la temporada finalizada {TemporadaId}", insigniasAsignadas, temporadaId);
                }
                else
                {
                    await transaction.RollbackAsync(ct);
                    _log.LogInformation("No se asignaron nuevas insignias para la temporada {TemporadaId}", temporadaId);
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                _log.LogError(ex, "Error al asignar insignias para la temporada {TemporadaId}", temporadaId);
                throw;
            }
        }
    }
}
