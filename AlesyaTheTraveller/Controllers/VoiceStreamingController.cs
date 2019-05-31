using AlesyaTheTraveller.Entities;
using AlesyaTheTraveller.Extensions;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace AlesyaTheTraveller.Controllers
{
    [Route("api/[controller]")]
    public class VoiceStreamingController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly CorvegaContext _context;

        public VoiceStreamingController(IConfiguration configuration, CorvegaContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        [HttpGet]
        public IActionResult SayHello()
        {
            return new JsonResult("42");
        }

        [EnableCors("AllowOrigin")]
        [HttpPost]
        public async Task<IActionResult> GetAudioAsync(string initialMessage, string substitution)
        {
            var message = GetRandomMessage(initialMessage);

            string FolderID = _configuration["Yandex:FolderId"];
            string IAM_TOKEN = _configuration["Yandex:IamToken"];
            string URL = "https://tts.api.cloud.yandex.net/speech/v1/tts:synthesize";

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {IAM_TOKEN}");

            var values = new Dictionary<string, string>
            {
                { "text", message.ReplaceForEach(substitution) },
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

        private string GetRandomMessage(string initialMessage)
        {
            var trashWords = new[] { "this", "that", "is", "are", "the" };
            var parts = initialMessage.ReplaceForEach(trashWords).Split(" ");
            var coeff = 1.0 / parts.Length;

            var utterances = GetUtterances(parts);

            if (!utterances.Any())
                return null;

            var a = utterances
                .Select(x => new { Message = x.ToLower(), Coeff = coeff })
                .GroupBy(x => x.Message.ToLower())
                .Select(x => x.Aggregate(new { Message = x.Key, Coeff = 0.0 }, (v, next) => new { v.Message, Coeff = v.Coeff + next.Coeff }));

            var messages = a.Where(x => x.Coeff == a.Max(v => v.Coeff)).ToArray();

            var random = new Random();
            return messages[random.Next(messages.Length)].Message;
        }

        private IEnumerable<string> GetUtterances(string[] values)
        {
            return _context.Responses
                .Where(x => values.Contains(x.Word) || values.Select(v => v.Substring(0, v.Length - 1)).Contains(x.Word))
                .Include(x => x.Resp)
                .AsEnumerable()
                .Select(x => x.Resp)
                .Select(x => x.Content);
        }
    }
}
