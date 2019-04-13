using AlesyaTheTraveller.Entities;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.IO;
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

        [EnableCors("AllowOrigin")]
        [HttpGet]
        public IActionResult SayHello()
        {
            return new OkResult();
        }

        [EnableCors("AllowOrigin")]
        [HttpPost]
        public async Task<IActionResult> GetAudioAsync(string message)
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

            if (!response.IsSuccessStatusCode)
                return NoContent();

            var fileName = "voice-response.ogg";
            var responseStream = await response.Content.ReadAsStreamAsync();
            var audioArray = ReadFully(responseStream);
            return Ok(new { response = audioArray, contentType = "audio/ogg", fileName });
        }

        private byte[] ReadFully(Stream stream)
        {
            byte[] buffer = new byte[32768];
            using (MemoryStream ms = new MemoryStream())
            {
                while (true)
                {
                    int read = stream.Read(buffer, 0, buffer.Length);
                    if (read <= 0)
                        return ms.ToArray();
                    ms.Write(buffer, 0, read);
                }
            }
        }
    }
}
