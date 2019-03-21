using Google.Protobuf;
using Grpc.Core;
using System;
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

        private const string FolderID = "b1gfb1uihgi76nm570vu";
        private const string IAM_TOKEN = "CggaATEVAgAAABKABLXYYExOGO4KrQ2l6fe9m2KizTFp7Y400DMJu_NpEgh40b0144jKcxk6wHQMgNsDBy8aYqWsO6EBmH1JKGkX9AzssmzlQB0RlR1iMrezQ_5XVgL-6Dt-T6n4jVstQoih1H16o3IINkvl7WFjRNsNe619LK4pxtRUrl-HyB9Q9JWh-atdxO_vKPaNzr8PEtLm0dHoihpUA3P2YOWaSkJdHQNWMwcRlHGIDGF_eD6vrlTWUb56Ps_bKqoRQr7XH-IWULY8ILrhdYD6VgYC-4J7W8zf7vi74NIc1ziVM79osLNPFTM-qiekfX9aFBvNC0x5kDLdv_AIDfLo7H6Rfhxwf1Aa9PbPC6CUU120cJEhLE1h0pq9aWHvVHv5IMIczmsn_7hfAVbjjDBGUBE0Ttt83qjMLKySGJYeInk8fn7CreCljZMF3V1WuWP8qsAeIYh7Kx8uX1xgq0wWFn5ADxd3rURVQwSVbrIAUzT2dRLsApTmj3Lb01AaRTzByxMzamW7Q4r6VT8qFiuHp3ug3PeTGuXiF49ofHC19qSG2UYZ1015-zVTBDLbSbtzGlJG4_7EXj-gOjhHTBuUJd0thb1we4p6yr3NM-HOEcxopo22fqSJHP1B8PSUAqLthfty2qNdqkhs4yuWpUV27ugb8ykfYqot_qi6SONpeHZsgZjucqiSGmAKIDVhYTU5MTk3YjQwNjRlNjg5ZjExZWY4OWJkNzcxNmZjENXez-QFGJWw0uQFIh4KFGFqZWxuYjY2Yjg3cTc5a2Z2Z2xzEgZFLjFldW1aADACOAFKCBoBMRUCAAAAUAEg7wQ";
        private const string TRANSLATE_API_KEY = "trnsl.1.1.20190311T181408Z.4a0d3e2df91c4d25.7eec30933d8ab387cbbfc4e88619b1ed576aa616";
        private const string APP_ID = "6ef251ce-0f25-489f-9169-9fde31f76024";
        private const string LUIS_API_KEY = "a57183c4362242f28dad63a5c1b9f959";
        private const int SAMPLE_RATE = 16000;
        private const short SILENCE_THRESHOLD = 5000;

        public static async Task<object> Recognize(CancellationToken token)
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
                while (await streamingCall.ResponseStream.MoveNext(default))
                {
                    foreach (var chunk in streamingCall.ResponseStream.Current.Chunks)
                    {
                        foreach (var alternative in chunk.Alternatives)
                        {
                            System.Diagnostics.Debug.WriteLine($"**************************************{alternative.Confidence}: {alternative.Text}");
                        }
                    }
                }
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
                }
            };

            //while (TestVar)
            //{
            //    await Task.Delay(25);
            //}
            await Task.Delay(5000);
            //Console.WriteLine("RECORDING STOPPED");
            //lock (writeLock) isWriteActive = false;

            await streamingCall.RequestStream.CompleteAsync();
            await printResponses;
            return 0;
        }

        public static void ProcessData(byte[] data)
        {
            OnDataAvailable?.Invoke(new VoiceStreamArgs(data, data.Length));
        }
    }
}
