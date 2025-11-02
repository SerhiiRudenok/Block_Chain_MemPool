using Microsoft.AspNetCore.SignalR;

namespace Block_Chain_Example_1.Hubs
{
    public class MiningHub : Hub
    {
        public async Task SendProgress(int nonce)
        {
            await Clients.All.SendAsync("ReceiveProgress", nonce);
        }
    }
}