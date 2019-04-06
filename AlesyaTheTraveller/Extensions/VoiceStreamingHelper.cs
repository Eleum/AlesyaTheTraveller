using AlesyaTheTraveller.Entities;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.AspNetCore.SignalR;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Yandex.Cloud.Ai.Stt.V2;

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
        private const string IAM_TOKEN = "CggaATEVAgAAABKABFmTNXjSnFAMTelozGo6Kbx2k5FjqvWvC6ufDq4IKNWG0sAzbapAyH3ZcxH7x0qtT853o2BdNYx6nWlghNmjryizYbVX2J0bA2n3sU-rwCn044JaXG94aHLlReszQ0fUD7dydhLsKABz1fW7czz1hPsiuwpq7pwMwFUWSY_hcgoTLW9EBNkdqtGWKv5jQhtcUzV-toZzvdUNvwNbnui0A6kk0I-J24ZmSNveUSyVKREXJVWHmiQXQIRmLyiQu16O9Jpauuhtc-5_Py-pL6UEDqOQWYz1kXytBYHTmcdusVzPJPlDcWp0ximu-rMZVlaBKcYZ0R89myTSLnHw4vgtmR9ts58axFvMzgVnbQCKdCsRohRH4M1-qg16G8VgRkRBdim4V8AeU_rUzduQKLu_hJt8OeEOec-Avo4SjpDr6aKOePxisG6Qm3XLsYQWu8NRFdInICgz4rWCqunVXbwdahqGTkLabihAap6Wp75WUstvijBqBHdxxx7GMnkACooeM0W7ViD7LaYeUKKnnzvmsXCIuP41h6NlEZ_dLJhJogiP1YZBL87wT6WbfJ96nvS_3-Nvuiey1W-H9H6R9bpXGxUKpeOYgFQIjO_2yXF9apt-ObQHvEljph3rnEr8tO4j-ZurwOmw_8lG44tn3MzA3D-rs1zztUR5jkeBDti_2rmlGmAKIGY4YmQzZTRkMmRhZTQ2YmJhZjkyYmNhYjRmNGY3NGQ1EP_GpOUFGL-Yp-UFIh4KFGFqZWxuYjY2Yjg3cTc5a2Z2Z2xzEgZFLjFldW1aADACOAFKCBoBMRUCAAAAUAEg7wQ";
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
                    //System.Diagnostics.Debug.WriteLine(MaxUtterance);
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
    }
}