using AlesyaTheTraveller.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace AlesyaTheTraveller.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private IHubContext<ChatHub> _hubContext;

        public ChatController(IHubContext<ChatHub> hubContext)
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
