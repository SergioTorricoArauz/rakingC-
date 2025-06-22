namespace RankingCyY.Domain
{
    public interface ITemporadaDomainService
    {
        Task AsignarInsigniasTemporadaActivaAsync(CancellationToken ct);
    }
}
