using AlesyaTheTraveller.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace AlesyaTheTraveller.Controllers
{
    [Route("api/[controller]")]
    public class VoiceStreamingController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public VoiceStreamingController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<HttpResponseMessage> GetAudioAsync([FromBody]string message)
        {
            string FolderID = _configuration["Yandex:FolderId"];
            string IAM_TOKEN = _configuration["Yandex:IamToken"];
            string URL = "https://tts.api.cloud.yandex.net/speech/v1/tts:synthesize";

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
            return new FileResponse(responseBytes, "audio/ogg", "voice-response.ogg");
        }
    }
}
