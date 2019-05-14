using System;
using System.Threading.Tasks;
using AlesyaTheTraveller.Extensions;
using AlesyaTheTraveller.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;

namespace AlesyaTheTraveller.Entities
{
    public class VoiceStreamingHub : Hub
    {
        private readonly VoiceStreamingHelper helper;

        public VoiceStreamingHub(IHubContext<VoiceStreamingHub> context, IFlightDataService flightData, IFlightDataCacheService flightDataCache, IConfiguration configuration)
        {
            helper = new VoiceStreamingHelper(context, flightData, flightDataCache, configuration);
        }

        public async Task StartRecognition(string input)
        {
            await Task.Run(() => helper.Recognize(helper.CancellationTokenSource.Token, input).ConfigureAwait(false));
        }

        public void ProcessVoiceStream(string base64Array)
        {
            var buffer = Convert.FromBase64String(base64Array);
            helper.ProcessData(buffer);
        }
    }
}