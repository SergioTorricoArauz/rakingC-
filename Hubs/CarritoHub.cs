using Microsoft.AspNetCore.SignalR;

namespace RankingCyY.Hubs
{
    public class CarritoHub : Hub
    {
        public async Task JoinCarritoGroup(string clienteId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Carrito_{clienteId}");
        }

        public async Task LeaveCarritoGroup(string clienteId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Carrito_{clienteId}");
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}