using AlesyaTheTraveller.Extensions;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AlesyaTheTraveller.Entities
{
    public class VoiceStreamingHub : Hub
    {
        private VoiceStreamingHelper helper;

        public VoiceStreamingHub(IHubContext<VoiceStreamingHub> context)
        {
            helper = new VoiceStreamingHelper(context);
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
