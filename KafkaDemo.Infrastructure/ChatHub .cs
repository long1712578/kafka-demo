using Microsoft.AspNetCore.SignalR;

namespace KafkaDemo.Infrastructure
{
    /// <summary>
    /// Class representing a SignalR hub for real-time chat functionality. It family endpoints for sending messages.
    /// </summary>
    public class ChatHub : Hub
    {
        public async Task SendMessage(string user, string message)
        {
            /// <summary> Allow clients to send messages to clients connected to the hub.
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }
    }
}
