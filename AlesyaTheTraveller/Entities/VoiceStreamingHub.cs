using Google.Protobuf;
using Grpc.Core;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Yandex.Cloud.Ai.Stt.V2;

namespace AlesyaTheTraveller.Entities
{
    public class VoiceStreamingHub : Hub
    {
        private class VoiceStreamArgs
        {
            public byte[] Buffer { get; set; }
            public int BytesRecorded { get; set; }

            public VoiceStreamArgs(byte[] buffer, int bytesRecorded)
            {
                Buffer = buffer;
                BytesRecorded = bytesRecorded;
            }
        }

        private delegate void DataAvailable(VoiceStreamArgs args);
        private event DataAvailable OnDataAvailable;

        private const string FolderID = "b1gfb1uihgi76nm570vu";
        private const string IAM_TOKEN = "CggaATEVAgAAABKABB34wZAgHmm3_gvPT-qxCNNaX_IM_oc5xN6a2jLyp8hhbHOm3K4lPaw70Y8sTqh5lwm-RJfB5Ln5Qyt-rSQC39oUVY7V0GQ6C1XBObKBspcZDfWFNXNKoNVl_xB5KAU3m7SNLC8iVgGec751N4nsMIM5pNkhAZmRjWWRCEM6ckB9aYaWj6yUqfdNJuaBtW60f-p-oPeeq1XfbFMoPL_mYDAMTqSyPjQU2OheXtwxpMc_Hm9kzuUKnKJqv6pkOtCguecIKLhWVEXojivUEECj_O3a7Oy4SPK419R2SpUUvLAKbKtyuQ1e-cN2toWLm0528LQdVhMyC0ILo7ZqeUA5h9MZnX6vBjXlu6Pq3uoE-vAAXMeBnGJF0Dj3yCMUQHDMuQL6tF7ChwL6sFpwcpo1gml_SFxhGV_9jsUVWXqkh2ukcElyU0N2UKSts0WM7RvCPHFhOe5qSbOCO6Es1RSQItwgMgLidZW4zECfke_j0DJwc9e8rChCa6XDUZtNP3glG3fYAxZuHpzmaumoq-jNj7zxGuMNNPZlO7x8TMM21vK4t3q8SiUY2PIWzJUAy2Ploysa1L3Ya79sVSv-rMxdt7ZiBYLokP7Vc8hcJJ0gdVBbNJTIWNvsC1bGNzwZdhHIiNZnn-HKmgL9AOaJFEpWy-WlXwp_Cfyfy0y-06xruZvhGmAKIDVkNjg3MmI4MjcxMzQ5MGRhNTAwMzMyNWFjYzc2NDUzELHMuuQFGPGdveQFIh4KFGFqZWxuYjY2Yjg3cTc5a2Z2Z2xzEgZFLjFldW1aADACOAFKCBoBMRUCAAAAUAEg7wQ";
        private const string TRANSLATE_API_KEY = "trnsl.1.1.20190311T181408Z.4a0d3e2df91c4d25.7eec30933d8ab387cbbfc4e88619b1ed576aa616";
        private const string APP_ID = "6ef251ce-0f25-489f-9169-9fde31f76024";
        private const string LUIS_API_KEY = "a57183c4362242f28dad63a5c1b9f959";
        private const int SAMPLE_RATE = 16000;
        private const short SILENCE_THRESHOLD = 5000;
        private Task RecognitionTask { get; set; }

        public void StartRecognition()
        {
            RecognitionTask = Task.Run(async () => await Recognize(new CancellationTokenSource().Token));
        }

        public void GetVoiceStream(string base64Array)
        {
            var buffer = Convert.FromBase64String(base64Array);
            OnDataAvailable?.Invoke(new VoiceStreamArgs(buffer, buffer.Length));
        }

        private async Task<object> Recognize(CancellationToken token)
        {
            var spec = new RecognitionSpec
            {
                LanguageCode = "ru-RU",
                ProfanityFilter = false,
                Model = "general",
                PartialResults = false,
                AudioEncoding = RecognitionSpec.Types.AudioEncoding.Linear16Pcm,
                SampleRateHertz = 16000
            };
            var streamingConfig = new RecognitionConfig
            {
                Specification = spec,
                FolderId = FolderID
            };
            var metadata = new Metadata
            {
                { "authorization", $"Bearer {IAM_TOKEN}" }
            };

            var channelCredentials = new SslCredentials();
            var channel = new Channel("stt.api.cloud.yandex.net", 443, channelCredentials);

            var client = new SttService.SttServiceClient(channel);
            var streamingCall = client.StreamingRecognize(metadata);
            await streamingCall.RequestStream.WriteAsync(
                new StreamingRecognitionRequest()
                {
                    Config = streamingConfig
                });

            Task printResponses = Task.Run(async () =>
            {
                while(await streamingCall.ResponseStream.MoveNext(default))
                {
                    foreach(var chunk in streamingCall.ResponseStream.Current.Chunks)
                    {
                        foreach(var alternative in chunk.Alternatives)
                        {
                            Console.WriteLine($"**************************************{alternative.Confidence}: {alternative.Text}");
                        }
                    }
                }
            });

            object writeLock = new object();
            bool isWriteActive = true;
            OnDataAvailable += (args) =>
            {
                short GetMaxBufferValue(byte[] recordedBuffer, int recordedBytes)
                {
                    short[] buffer = new short[recordedBytes / 2];
                    Buffer.BlockCopy(args.Buffer, 0, buffer, 0, args.BytesRecorded);
                    return buffer.Max();
                }

                lock (writeLock)
                {
                    Console.WriteLine($"**************************************HERE1");
                    if (!isWriteActive) return;
                    Console.WriteLine($"**************************************HERE2");
                    streamingCall.RequestStream.WriteAsync(
                        new StreamingRecognitionRequest()
                        {
                            AudioContent = ByteString.CopyFrom(args.Buffer, 0, args.BytesRecorded)
                        }).Wait();
                }
            };

            await Task.Delay(6000);
            Console.WriteLine("RECORDING STOPPED");
            lock (writeLock) isWriteActive = false;
            
            await streamingCall.RequestStream.CompleteAsync();
            await printResponses;
            await RecognitionTask;
            return 0;
        }
    }
}
