using AlesyaTheTraveller.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

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

        [HttpPost]
        public async Task<HttpResponseMessage> GetAudioAsync(string message)
        {
            const string FolderID = "b1gfb1uihgi76nm570vu";
            const string IAM_TOKEN = "CggaATEVAgAAABKABJCOiAl34jOZrOg7139s9VeoBAVho8jB--BipTWiwZY_WXbJE4CzwZpr9XQwkd2h1wLBVyreU1clSWIoThHxAv8JZqsDGqvl-su34y92e7_doarIvwyANTbfYkqanborSkXwNn5QBAm-oxgZVBCg1VtmQFIZKV0Ek9SyVTFjD4sBiTxAD6n1An0d41Z-mjMDaQ-CxQMpswR2-ipPPe_TrAoL6YeHuH5uY_dJ_VxwciLJ52J7QkaLRDXPIWNLwMdSEIqcAXUYL_fQyZsgllPDQ2QxHqx2rn2DezUil3Ecu9WqVvktFyBFJb8k1BIYZbAaSLrTaDuiyn67yUoFBfNnity4yDoBnoiZ4BUn81ssod40doqHXzwpYr541KenKZy1RzelsHG3-xZuXBfZi4fgFGmQ2y_Zx1qBpb64haL5AjODEjgz2GhXBGpGFmLu3bHZKrY2VPw9Essy7OX5N3vVHqPcpd4l1ddtq7fRonjfT7f4ellbacF65OYLQIxzjpXXCDqEn0e90_NbzCYSZowi543z01vXic7wmq35S1nN5MJ3VCPx-ZF4ibFd3Q5ERDpMQ6CmAL5QbrhDYb2pIPp6WZIR1OpGWK3CHvnQZqi_wvx2Kir9s2AFbHXIiiVIfH13eXBlOaUDghgZS5TVSsolEGBRmYXgs5mLQ3p_9ux03yTsGmAKIGM1OTk0ZDg1M2I4ZTQyNDRhYjZjNTUwNTg2NGU4YjgxEJ36s-UFGN3LtuUFIh4KFGFqZWxuYjY2Yjg3cTc5a2Z2Z2xzEgZFLjFldW1aADACOAFKCBoBMRUCAAAAUAEg7wQ";
            const string URL = "https://tts.api.cloud.yandex.net/speech/v1/tts:synthesize";

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {IAM_TOKEN}");

            var values = new Dictionary<string, string>
            {
                { "text", message },
                { "lang", "ru-RU" },
                { "folderId", FolderID }
            };

            var content = new FormUrlEncodedContent(values);
            var response = await client.PostAsync(URL, content);
            var responseBytes = await response.Content.ReadAsByteArrayAsync();
            System.IO.File.WriteAllBytes("voice-response.ogg", responseBytes);

            return null;
        }
    }
}
