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
        private Task RecognitionTask { get; set; }
        private SynchronizationContext context;
        public ISynchronizeInvoke SynchronizingObject { get; set; }

        //private object writeLock = new object();
        //private AsyncDuplexStreamingCall<StreamingRecognitionRequest, StreamingRecognitionResponse> streamingCall;
        //private bool IsWriteActive { get; set; }

        private EventHandler _syncEvent;

        public event EventHandler SyncEvent
        {
            add
            {
                if (!(value.Target is ISynchronizeInvoke))
                {
                    throw new ArgumentException();
                }
                _syncEvent = (EventHandler)Delegate.Combine(_syncEvent, value);
            }
            remove
            {
                _syncEvent = (EventHandler)Delegate.Remove(_syncEvent, value);
            }
        }

        public void RaiseMyEvent()
        {
            if (_syncEvent == null) return;
            foreach(EventHandler handler in _syncEvent.GetInvocationList())
            {
                var capture = handler;
                var syncronizingObject = (ISynchronizeInvoke)handler.Target;
                syncronizingObject.Invoke((Action)(() => capture(this, new EventArgs())), null);
            }
        }

        private short GetMaxBufferValue(byte[] recordedBuffer, int recordedBytes)
        {
            short[] buffer = new short[recordedBytes / 2];
            Buffer.BlockCopy(recordedBuffer, 0, buffer, 0, recordedBytes);
            return buffer.Max();
        }

        public bool TestVar { get; set; } = true;

        static VoiceStreamingHub()
        {
        }

        //private Task StartMethodInner()
        //{
        //    Console.WriteLine("METHOD STARTED");

        //    var tcs = new TaskCompletionSource<object>();

        //    context.Post(s => OnTestCalled += () => tcs.SetResult(true), null);
        //    return tcs.Task;
        //}

        public async Task StartMethod()
        {
            //Task.Run(async () =>
            //{
            //    await StartMethodInner();
            //    Console.WriteLine("METHOD FINISHED");
            //});
        }

        //public void Test()
        //{
        //    OnTestCalled?.Invoke();
        //}

        public void SetVariableFalse()
        {
            TestVar = false;
        }

        public ChannelReader<int> DelayCounter(int delay)
        {
            var channel = System.Threading.Channels.Channel.CreateUnbounded<int>();
            _ = WriteItems(channel.Writer, 20, delay);
            return channel.Reader;
        }

        private async Task WriteItems(ChannelWriter<int> writer, int count, int delay)
        {
            for (int i = 0; i < count; i++)
            {
                if (i % 5 == 0)
                    delay *= 2;

                await writer.WriteAsync(i);
                await Task.Delay(delay);
            }

            writer.TryComplete();
        }

        public async Task StartRecognition()
        {
            Task.Run(async () => await VoiceStreamingHelper.Recognize(new CancellationTokenSource().Token));
        }

        public void ProcessVoiceStream(string base64Array)
        {
            var buffer = Convert.FromBase64String(base64Array);
            VoiceStreamingHelper.ProcessData(buffer);
        }
    }
}
