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
                .Where(p => p.TemporadaId == temporada.Id)
                .GroupBy(p => p.ClienteId)
                .Select(g => new { ClienteId = g.Key, Puntos = g.Sum(x => x.Puntos) })
                .OrderByDescending(r => r.Puntos)
                .Take(3)
                .ToListAsync(ct);

            if (ranking.Count == 0) return;

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
    }
}
