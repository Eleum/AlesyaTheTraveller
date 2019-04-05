using AlesyaTheTraveller.Extensions;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Yandex.Cloud.Ai.Stt.V2;

namespace AlesyaTheTraveller.Entities
{
    public class VoiceStreamingHub : Hub
    {
        public async Task StartRecognition()
        {
            await Task.Run(() => VoiceStreamingHelper.Recognize(new CancellationTokenSource().Token).ConfigureAwait(false));
        }

        public void ProcessVoiceStream(string base64Array)
        {
            var buffer = Convert.FromBase64String(base64Array);
            VoiceStreamingHelper.ProcessData(buffer);
        }
    }
}
