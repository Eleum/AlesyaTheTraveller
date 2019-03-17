using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlesyaTheTraveller.Entities
{
    public class VoiceStreamingHub : Hub
    {
        public async Task GetVoiceStream(string base64Array)
        {
            Console.WriteLine(Convert.FromBase64String(base64Array)); //8192
        }
    }
}
