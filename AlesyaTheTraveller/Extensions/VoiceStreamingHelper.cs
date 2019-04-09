using AlesyaTheTraveller.Entities;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Yandex.Cloud.Ai.Stt.V2;
using YandexTranslateCSharpSdk;

namespace AlesyaTheTraveller.Extensions
{
    public class VoiceStreamingHelper
    {
        private readonly IHubContext<VoiceStreamingHub> _context;
        public CancellationTokenSource CancellationTokenSource { get; private set; }

        public class VoiceStreamArgs : EventArgs
        {
            public byte[] Buffer { get; set; }
            public int BytesRecorded { get; set; }

            public VoiceStreamArgs(byte[] buffer, int bytesRecorded)
            {
                Buffer = buffer;
                BytesRecorded = bytesRecorded;
            }
        }
        public static event EventHandler<VoiceStreamArgs> OnDataAvailable;

        private const string FolderID = "b1gfb1uihgi76nm570vu";
        private const string IAM_TOKEN = "CggaATEVAgAAABKABJCOiAl34jOZrOg7139s9VeoBAVho8jB--BipTWiwZY_WXbJE4CzwZpr9XQwkd2h1wLBVyreU1clSWIoThHxAv8JZqsDGqvl-su34y92e7_doarIvwyANTbfYkqanborSkXwNn5QBAm-oxgZVBCg1VtmQFIZKV0Ek9SyVTFjD4sBiTxAD6n1An0d41Z-mjMDaQ-CxQMpswR2-ipPPe_TrAoL6YeHuH5uY_dJ_VxwciLJ52J7QkaLRDXPIWNLwMdSEIqcAXUYL_fQyZsgllPDQ2QxHqx2rn2DezUil3Ecu9WqVvktFyBFJb8k1BIYZbAaSLrTaDuiyn67yUoFBfNnity4yDoBnoiZ4BUn81ssod40doqHXzwpYr541KenKZy1RzelsHG3-xZuXBfZi4fgFGmQ2y_Zx1qBpb64haL5AjODEjgz2GhXBGpGFmLu3bHZKrY2VPw9Essy7OX5N3vVHqPcpd4l1ddtq7fRonjfT7f4ellbacF65OYLQIxzjpXXCDqEn0e90_NbzCYSZowi543z01vXic7wmq35S1nN5MJ3VCPx-ZF4ibFd3Q5ERDpMQ6CmAL5QbrhDYb2pIPp6WZIR1OpGWK3CHvnQZqi_wvx2Kir9s2AFbHXIiiVIfH13eXBlOaUDghgZS5TVSsolEGBRmYXgs5mLQ3p_9ux03yTsGmAKIGM1OTk0ZDg1M2I4ZTQyNDRhYjZjNTUwNTg2NGU4YjgxEJ36s-UFGN3LtuUFIh4KFGFqZWxuYjY2Yjg3cTc5a2Z2Z2xzEgZFLjFldW1aADACOAFKCBoBMRUCAAAAUAEg7wQ";
        private const string TRANSLATE_API_KEY = "trnsl.1.1.20190311T181408Z.4a0d3e2df91c4d25.7eec30933d8ab387cbbfc4e88619b1ed576aa616";
        private const string APP_ID = "6ef251ce-0f25-489f-9169-9fde31f76024";
        private const string LUIS_API_KEY = "a57183c4362242f28dad63a5c1b9f959";

        private const short SILENCE_THRESHOLD = 5000;
        private static short MaxUtterance = -1;

        public VoiceStreamingHelper(IHubContext<VoiceStreamingHub> context)
        {
            _context = context;
            CancellationTokenSource = new CancellationTokenSource();
        }

        public async Task<object> Recognize(CancellationToken token)
        {
            var spec = new RecognitionSpec
            {
                LanguageCode = "ru-RU",
                ProfanityFilter = false,
                Model = "general",
                PartialResults = false,
                AudioEncoding = RecognitionSpec.Types.AudioEncoding.Linear16Pcm,
                SampleRateHertz = 48000
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

            var writeLock = new object();
            bool isWriteActive = true;

            OnDataAvailable += (sender, args) =>
            {
                lock (writeLock)
                {
                    if (!isWriteActive) return;

                    streamingCall.RequestStream.WriteAsync(
                        new StreamingRecognitionRequest()
                        {
                            AudioContent = ByteString.CopyFrom(args.Buffer, 0, args.BytesRecorded)
                        }).Wait();

                    MaxUtterance = GetMaxBufferValue(args.Buffer, args.BytesRecorded);
                }
            };

            var worker = new BackgroundWorker
            {
                WorkerSupportsCancellation = true
            };
            worker.DoWork += (sender, args) =>
            {
                var timeout = 2500;
                var currentTime = 0;

                while (!args.Cancel)
                {
                    //System.Diagnostics.Debug.WriteLine($"working... current time is {currentTime}");
                    if (worker.CancellationPending || currentTime >= timeout)
                    {
                        args.Cancel = true;
                        //System.Diagnostics.Debug.WriteLine("recording stopped from background worker");
                        return;
                    }

                    if (MaxUtterance < SILENCE_THRESHOLD)
                    {
                        currentTime += 100;
                    }
                    else
                    {
                        currentTime = 0;
                    }
                    Thread.Sleep(100);
                }
            };
            worker.RunWorkerCompleted += (sender, args) =>
            {
                _context.Clients.All.SendAsync("InvokeStopVoiceStream");
                CancellationTokenSource.Cancel();
            };

            var printWorker = new BackgroundWorker
            {
                WorkerSupportsCancellation = true
            };
            printWorker.DoWork += async (sender, args) =>
            {
                try
                {
                    while (await streamingCall.ResponseStream.MoveNext(default))
                    {
                        foreach (var chunk in streamingCall.ResponseStream.Current.Chunks)
                        {
                            foreach (var alternative in chunk.Alternatives)
                            {
                                System.Diagnostics.Debug.WriteLine($"***************************{alternative.Confidence}: {alternative.Text}");
                                var englishVersion = await TranslateMessageAsync(alternative.Text);
                                System.Diagnostics.Debug.WriteLine($"***************************{alternative.Confidence}: {englishVersion}");

                                var tasks = new List<Task>
                                {
                                    _context.Clients.All.SendAsync("BroadcastMessageRuEng", $"RU - {alternative.Text}\nENG - {englishVersion}"),
                                    _context.Clients.All.SendAsync("SayVoiceMessage", $"Хех, вы скаазали {alternative.Text}"),
                                };

                                await Task.WhenAll(tasks);
                            }
                        }
                    }
                }
                catch(Exception e)
                {
                    System.Diagnostics.Debug.WriteLine("EXCEPTION");
                }
            };

            worker.RunWorkerAsync();
            printWorker.RunWorkerAsync();

            while(!token.IsCancellationRequested)
            {
                await Task.Delay(25);
            }

            //System.Diagnostics.Debug.WriteLine("RECORDING STOPPED");
            lock (writeLock) isWriteActive = false;

            await streamingCall.RequestStream.CompleteAsync();
            return 0;
        }

        public void ProcessData(byte[] data)
        {
            OnDataAvailable?.Invoke(this, new VoiceStreamArgs(data, data.Length));
        }

        private short GetMaxBufferValue(byte[] recordedBuffer, int recordedBytes)
        {
            short[] buffer = new short[recordedBytes / 2];
            Buffer.BlockCopy(recordedBuffer, 0, buffer, 0, recordedBytes);
            return buffer.Max();
        }

        private async Task<string> TranslateMessageAsync(string message)
        {
            var translator = new YandexTranslateSdk
            {
                ApiKey = TRANSLATE_API_KEY
            };

            return await translator.TranslateText(message, "ru-en");
        }
    }
}