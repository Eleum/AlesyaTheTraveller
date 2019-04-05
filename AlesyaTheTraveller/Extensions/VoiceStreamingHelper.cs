using Google.Protobuf;
using Grpc.Core;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Yandex.Cloud.Ai.Stt.V2;

namespace AlesyaTheTraveller.Extensions
{
    public static class VoiceStreamingHelper
    {
        public class VoiceStreamArgs
        {
            public byte[] Buffer { get; set; }
            public int BytesRecorded { get; set; }

            public VoiceStreamArgs(byte[] buffer, int bytesRecorded)
            {
                Buffer = buffer;
                BytesRecorded = bytesRecorded;
            }
        }
        public delegate void VoiceStreaming(VoiceStreamArgs data);
        public static event VoiceStreaming OnDataAvailable;

        
        private const short SILENCE_THRESHOLD = 5000;

        private static short MaxUtterance = -1;

        public static async Task<object> Recognize(CancellationToken token)
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

            OnDataAvailable += (args) =>
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
                    System.Diagnostics.Debug.WriteLine($"working... current time is {currentTime}");
                    if (worker.CancellationPending || currentTime > timeout)
                    {
                        args.Cancel = true;
                        System.Diagnostics.Debug.WriteLine("recording stopped from background worker");
                        return;
                    }

                    if (MaxUtterance < SILENCE_THRESHOLD)
                    {
                        currentTime += 200;
                    }
                    else
                    {
                        currentTime = 0;
                    }
                    Thread.Sleep(200);
                }
            };
            worker.RunWorkerCompleted += (sender, args) => System.Diagnostics.Debug.WriteLine("BW STOPPED");//CancellationTokenSource.Cancel();

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
                                System.Diagnostics.Debug.WriteLine($"{alternative.Confidence}: {alternative.Text}");
                            }
                        }
                    }
                }
                catch(Exception e)
                {
                    System.Diagnostics.Debug.WriteLine("EXCEPTION");
                }
            };
            printWorker.RunWorkerCompleted += (sender, args) => System.Diagnostics.Debug.WriteLine("PRINT STOPPED");

            //worker.RunWorkerAsync();
            printWorker.RunWorkerAsync();

            await Task.Delay(5000);
            System.Diagnostics.Debug.WriteLine("RECORDING STOPPED");

            lock (writeLock) isWriteActive = false;

            await streamingCall.RequestStream.CompleteAsync();
            return 0;
        }

        public static void ProcessData(byte[] data)
        {
            OnDataAvailable?.Invoke(new VoiceStreamArgs(data, data.Length));
        }

        static short GetMaxBufferValue(byte[] recordedBuffer, int recordedBytes)
        {
            short[] buffer = new short[recordedBytes / 2];
            Buffer.BlockCopy(recordedBuffer, 0, buffer, 0, recordedBytes);
            return buffer.Max();
        }
    }
}