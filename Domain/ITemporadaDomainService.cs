namespace RankingCyY.Domain
{
    public interface ITemporadaDomainService
    {
        Task AsignarInsigniasTemporadaActivaAsync(CancellationToken ct);
        Task AsignarInsigniasTemporadaFinalizadaAsync(int temporadaId, CancellationToken ct);
    }
}
