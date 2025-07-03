using Microsoft.AspNetCore.SignalR;

namespace RankingCyY.Hubs
{
    public class HistoriaHub : Hub
    {
        public async Task JoinHistoryGroup(string historiaId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Historia_{historiaId}");
        }

        public async Task LeaveHistoryGroup(string historiaId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Historia_{historiaId}");
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}