using AlesyaTheTraveller.Extensions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AlesyaTheTraveller.Entities
{
    public class VoiceStreamingHub : Hub
    {
        private readonly VoiceStreamingHelper helper;

        public VoiceStreamingHub(IHubContext<VoiceStreamingHub> context, IConfiguration configuration)
        {
            helper = new VoiceStreamingHelper(context, configuration);
        }

        public async Task StartRecognition()
        {
            await Task.Run(() => helper.Recognize(helper.CancellationTokenSource.Token).ConfigureAwait(false));
        }

        public void ProcessVoiceStream(string base64Array)
        {
            var buffer = Convert.FromBase64String(base64Array);
            helper.ProcessData(buffer);
        }
    }
}
