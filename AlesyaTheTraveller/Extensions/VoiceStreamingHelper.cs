using AlesyaTheTraveller.Entities;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
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
        private readonly string FOLDER_ID, IAM_TOKEN, TRANSLATE_API_KEY, LUIS_APP_URL, LUIS_APP_ID, LUIS_API_KEY;

        private short MaxUtterance = -1;
        public CancellationTokenSource CancellationTokenSource { get; private set; }

        public VoiceStreamingHelper(IHubContext<VoiceStreamingHub> context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;

            FOLDER_ID =  _configuration["Yandex:FolderId"];
            IAM_TOKEN = _configuration["Yandex:IamToken"];
            TRANSLATE_API_KEY = _configuration["Yandex:TranslateApiKey"];
            LUIS_APP_URL = _configuration["Luis:AppUrl"];
            LUIS_APP_ID = _configuration["Luis:AppId"];
            LUIS_API_KEY = _configuration["Luis:ApiKey"];

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
                FolderId = FOLDER_ID
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

            var silenceCheckWorker = new BackgroundWorker
            {
                WorkerSupportsCancellation = true
            };
            silenceCheckWorker.DoWork += (sender, args) =>
            {
                var timeout = 2500;
                var currentTime = 0;
                var silenceTreshold = 5000;

                while (!args.Cancel)
                {
                    //System.Diagnostics.Debug.WriteLine($"working... current time is {currentTime}");
                    if (silenceCheckWorker.CancellationPending || currentTime >= timeout)
                    {
                        args.Cancel = true;
                        //System.Diagnostics.Debug.WriteLine("recording stopped from background worker");
                        return;
                    }

                    if (MaxUtterance < silenceTreshold)
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
            silenceCheckWorker.RunWorkerCompleted += (sender, args) =>
            {
                _context.Clients.All.SendAsync("InvokeStopVoiceStream");
                CancellationTokenSource.Cancel();
            };

            var notifyWorker = new BackgroundWorker
            {
                WorkerSupportsCancellation = true
            };
            notifyWorker.DoWork += async (sender, args) =>
            {
                try
                {
                    while (await streamingCall.ResponseStream.MoveNext(default))
                    {
                        foreach (var chunk in streamingCall.ResponseStream.Current.Chunks)
                        {
                            // or use foreach (var alternative in chunk.Alternatives)

                            var alternative = chunk.Alternatives.First();
                            var englishAlternative = await TranslateMessageAsync(alternative.Text);
                            var intent = await GetMessageIntentAsync(englishAlternative);

                            var tasks = new List<Task>
                            {
                                _context.Clients.All.SendAsync("BroadcastMessageRuEng", $"RU - {alternative.Text}\nENG - {englishAlternative}"),
                                _context.Clients.All.SendAsync("BroadcastIntent", $"{intent}"),
                                _context.Clients.All.SendAsync("SayVoiceMessage", $"{alternative.Text}"),
                            };

                            await Task.WhenAll(tasks);

                            break;
                        }
                    }
                }
                catch(Exception e)
                {
                    System.Diagnostics.Debug.WriteLine("EXCEPTION");
                }
            };

            silenceCheckWorker.RunWorkerAsync();
            notifyWorker.RunWorkerAsync();

            while(!token.IsCancellationRequested)
                await Task.Delay(25);

            lock (writeLock)
                isWriteActive = false;

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

        private async Task<string> GetMessageIntentAsync(string message)
        {
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", LUIS_API_KEY);

            queryString["q"] = message;
            queryString["timezoneOffset"] = "0";
            queryString["verbose"] = "false";
            queryString["spellCheck"] = "false";
            queryString["staging"] = "true";

            var uri = LUIS_APP_URL + LUIS_APP_ID + "?" + queryString;
            var response = await client.GetAsync(uri);

            if(response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else
            {
                return "ERRORRRRRRRRRRRRRRRRRRRRR";
            }
        }
    }
}