using AlesyaTheTraveller.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace AlesyaTheTraveller.Controllers
{
    [Route("api/[controller]")]
    public class VoiceStreamingController : ControllerBase
    {
        private IHubContext<VoiceStreamingHub> _hubContext;

        public VoiceStreamingController(IHubContext<VoiceStreamingHub> hubContext)
        {
            _hubContext = hubContext;
        }

        //[HttpGet]
        //public async Task<IActionResult> Get()
        //{
        //    return Ok();
        //}

        //public async Task BroadcastMessage(string message)
        //{
        //    await _hubContext.Clients.All.SendAsync("broadcastmessage", message);
        //    return;
        //}
    }
}
