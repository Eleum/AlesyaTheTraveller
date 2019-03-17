using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlesyaTheTraveller.Entities
{
    public class ChatHub : Hub
    {
        public async Task BroadcastMessage(short[] message)
        {
            //await Clients.All.SendAsync("broadcastmessage", message);
            Console.WriteLine(message);
        }
    }
}
