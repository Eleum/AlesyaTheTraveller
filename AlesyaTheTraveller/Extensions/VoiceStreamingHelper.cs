using AlesyaTheTraveller.Entities;
using AlesyaTheTraveller.Services;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
        private readonly IFlightDataService _flightData;
        private readonly IFlightDataCacheService _flightDataCache;
        private readonly string FOLDER_ID, IAM_TOKEN, TRANSLATE_API_KEY, LUIS_APP_URL, LUIS_APP_ID, LUIS_API_KEY;

        private short MaxUtterance = -1;
        public CancellationTokenSource CancellationTokenSource { get; private set; }

        public VoiceStreamingHelper(IHubContext<VoiceStreamingHub> context, 
                                    IFlightDataService flightData, IFlightDataCacheService flightDataCache, 
                                    IConfiguration configuration)
        {
            _context = context;
            _flightData = flightData;
            _flightDataCache = flightDataCache;
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
            //Debug.WriteLine("METHOD STARTED");
            //var englishAlternative = await TranslateMessageAsync("билеты из Минска в Санкт-Петербург на 25 мая");
            //var proc = new IntentProcessor(new LuisConfig(LUIS_APP_URL, LUIS_API_KEY, LUIS_APP_ID), _flightDataCache, _flightData);

            //var intent = await proc.GetMessageIntentAsync(englishAlternative);
            //var intentParams = await proc.ParseIntent(intent);

            //await ProcessIntentParams(intentParams);

            //return null;

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
                var timeout = 1500;
                var currentTime = 0;
                var silenceTreshold = 5000;

                while (!args.Cancel)
                {
                    if (silenceCheckWorker.CancellationPending || currentTime >= timeout)
                    {
                        args.Cancel = true;
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
                            await _context.Clients.All.SendAsync("BroadcastMessageRuEng", $"RU - {alternative.Text}\nENG - {englishAlternative}");

                            var proc = new IntentProcessor(new LuisConfig(LUIS_APP_URL, LUIS_API_KEY, LUIS_APP_ID), _flightDataCache, _flightData);

                            var intent = await proc.GetMessageIntentAsync(englishAlternative);
                            var intentParams = await proc.ParseIntent(intent);

                            await ProcessIntentParams(intentParams);
                            break;
                        }
                    }
                }
                catch(Exception e)
                {
                    Debug.WriteLine("EXCEPTION");
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

        private async Task ProcessIntentParams(Dictionary<string, string> intentParams)
        {
            var type = intentParams.GetAndRemove("--type");

            switch(type)
            {
                case "Interaction":
                    if(intentParams.ContainsKey("item"))
                    {
                        await _context.Clients.All.SendAsync("SwitchToItem", intentParams["item"]);
                    }
                    else
                    {
                        await _context.Clients.All.SendAsync("SortData", intentParams["number"]);
                    }
                    break;
                case "Travelling":
                    var hotels = RunHotelSearch(intentParams["--destination"], intentParams["outboundDate"], _context);
                    var flights = RunFlightSearch(intentParams, _context);

                    await Task.WhenAny(flights, hotels);

                    // switch to flight-data component in client app
                    await _context.Clients.All.SendAsync("SwitchToItem", "flight");
                    break;
            }
        }

        private enum FetchType
        {
            Flight,
            Hotel
        }

        /// <summary>
        /// Searches flights based on dictionary filled params
        /// </summary>
        /// <param name="param">Search query parameters</param>
        /// <param name="context"></param>
        /// <returns></returns>
        private async Task RunFlightSearch(Dictionary<string, string> param, IHubContext<VoiceStreamingHub> context)
        {
            List<FlightData> flights = null;

            param.Remove("--destination");

            try
            {
                var tries = 3;
                while (tries > 0)
                {
                    await context.Clients.All.SendAsync("Notify", $"Попытка найти рейсы #{-tries+4}", 1);

                    var sessionId = await _flightData.CreateSession(param);
                    if(sessionId == null)
                    {
                        tries--;
                        continue;
                    }

                    flights = await _flightData.PollSessionResults(sessionId)
                        .ContinueWith((x) => _flightData.FormFlightData(x.Result),
                        TaskContinuationOptions.NotOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
                    break;
                }
            }
            catch(Exception e)
            {
                await context.Clients.All.SendAsync("Notify", $"{e.Message}\n{e.InnerException?.Message}", 3);
            }

            await context.Clients.All.SendAsync("Notify", $"Данные {(flights == null ? "не " : "")}получены!", flights == null ? 2 : 0);
            await _context.Clients.All.SendAsync("FetchData", flights, FetchType.Flight);
        }

        /// <summary>
        /// Searches hotels in destination city with arrival date as outboundDate
        /// </summary>
        /// <param name="destination">Destination city</param>
        /// <param name="outboundDate">Hotel arrival date</param>
        /// <param name="context">Hub context to communicate with client app</param>
        /// <returns></returns>
        private async Task RunHotelSearch(string destination, string outboundDate, IHubContext<VoiceStreamingHub> context)
        {
            var locations = await _flightData.GetLocations(destination);

            // negative values are ok
            var destinationId = locations.FirstOrDefault(x => x.DestinationType == "city")?.Id;

            HotelData[] hotelData = null;

            if (destinationId != null)
            {
                var tries = 5;
                while (tries > 0)
                {
                    try
                    {
                        hotelData = await _flightData.GetHotelData(destinationId.Value,
                            DateTime.ParseExact(outboundDate, "yyyy-MM-dd", null));

                        if (hotelData == null)
                        {
                            tries--;
                        }
                    }
                    catch (Exception e)
                    {
                        await context.Clients.All.SendAsync("Notify", $"{e.Message}\n{e.InnerException?.Message}", 3);
                        tries--;
                    }
                }
            }

            await _context.Clients.All.SendAsync("FetchData", hotelData, FetchType.Hotel);
        }
    }
}