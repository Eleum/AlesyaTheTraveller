using AlesyaTheTraveller.Entities;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
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

    public class VoiceStreamingHelper
    {
        public static event EventHandler<VoiceStreamArgs> OnDataAvailable;

        private readonly IConfiguration _configuration;
        private readonly IHubContext<VoiceStreamingHub> _context;
        private readonly string FolderID, IAM_TOKEN, TRANSLATE_API_KEY, APP_ID, LUIS_API_KEY;

        private const short SILENCE_THRESHOLD = 5000;
        private short MaxUtterance = -1;
        public CancellationTokenSource CancellationTokenSource { get; private set; }

        public VoiceStreamingHelper(IHubContext<VoiceStreamingHub> context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;

            FolderID =  _configuration["Yandex:FolderId"];
            IAM_TOKEN = _configuration["Yandex:IamToken"];
            TRANSLATE_API_KEY = _configuration["Yandex:TranslateApiKey"];
            LUIS_API_KEY = _configuration["Luis:ApiKey"];
            APP_ID = _configuration["Luis:AppId"];

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