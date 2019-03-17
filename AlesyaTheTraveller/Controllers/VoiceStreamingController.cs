using AlesyaTheTraveller.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace AlesyaTheTraveller.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VoiceStreamingController : ControllerBase
    {
        private IHubContext<VoiceStreamingHub> _hubContext;

        public VoiceStreamingController(IHubContext<VoiceStreamingHub> hubContext)
        {
            _hubContext = hubContext;
        }

        //public IActionResult Get()
        //{
        //    _hubContext.Clients.All.SendAsync("broadcastmessage", "123");
        //    return Ok(new { Message = "Request Completed" });
        //}

        //public async Task BroadcastMessage(string message)
        //{
        //    await _hubContext.Clients.All.SendAsync("broadcastmessage", message);
        //    return;
        //}
    }
}
