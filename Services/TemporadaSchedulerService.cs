using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RankingCyY.Data;
using RankingCyY.Domain;

namespace RankingCyY.Services;

public record SchedulerOptions
{
    public int IntervalMinutes { get; init; } = 60;
}

public class TemporadaSchedulerService(
    IServiceProvider sp,
    ILogger<TemporadaSchedulerService> log,
    IOptions<SchedulerOptions> opt) : BackgroundService
{
    private readonly IServiceProvider _sp = sp;
    private readonly ILogger<TemporadaSchedulerService> _log = log;
    private readonly TimeSpan _delay = TimeSpan.FromMinutes(opt.Value.IntervalMinutes);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await using var scope = _sp.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var dom = scope.ServiceProvider.GetRequiredService<ITemporadaDomainService>();

                await VerificarFinalizarAsync(db, ct);
                await VerificarActivarAsync(db, ct);
                await VerificarInsigniasAsync(dom, ct);   // ← ahora pasamos el servicio
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error en TemporadaSchedulerService");
            }

            await Task.Delay(_delay, ct);
        }
    }

    /* ----------  lógica separada ---------- */

    private async Task VerificarFinalizarAsync(AppDbContext db, CancellationToken ct)
    {
        var hoy = DateTime.UtcNow.Date;
        var activa = await db.Temporadas
            .FirstOrDefaultAsync(t => t.EstaDisponible && t.Fin.Date <= hoy, ct);

        _log.LogInformation("Verificando finalización: hoy={Hoy}, temporada activa encontrada={Activa}", hoy, activa != null);

        if (activa is null) return;

        activa.EstaDisponible = false;
        await db.SaveChangesAsync(ct);
        _log.LogInformation("Temporada {Id}-{Nombre} finalizada", activa.Id, activa.Nombre);
    }

    private async Task VerificarActivarAsync(AppDbContext db, CancellationToken ct)
    {
        var hoy = DateTime.UtcNow.Date;
        var hayActiva = await db.Temporadas.AnyAsync(t => t.EstaDisponible, ct);
        if (hayActiva) return;

        var siguiente = await db.Temporadas
            .Where(t => !t.EstaDisponible && t.Inicio.Date <= hoy && t.Fin.Date > hoy)
            .OrderBy(t => t.Inicio)
            .FirstOrDefaultAsync(ct);

        if (siguiente is null) return;

        siguiente.EstaDisponible = true;
        await db.SaveChangesAsync(ct);
        _log.LogInformation("Temporada {Id}-{Nombre} activada", siguiente.Id, siguiente.Nombre);
    }

    private static async Task VerificarInsigniasAsync(ITemporadaDomainService dom, CancellationToken ct)
    {
        await dom.AsignarInsigniasTemporadaActivaAsync(ct);
    }
}
