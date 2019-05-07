import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { Subject } from 'rxjs';
import * as signalR from '@aspnet/signalr';
import { RootObject } from '../flight-data/flight-data.component';

@Injectable({
  providedIn: 'root'
})
export class SignalRService {
  private hubConnection: signalR.HubConnection
  private stream: MediaStream;
  private audioCtx: AudioContext;

  public newMessageReceived = new Subject<string>();
  public voiceMessageReceived = new Subject<string>();
  public newIntentReceived = new Subject<string>();
  public flightDataFetched = new Subject<RootObject>();

  covertFloat32ToUInt8(buffer: Float32Array) {
    let l = buffer.length;
    let buf = new Int16Array(l);
    while (l--) {
      buf[l] = Math.min(1, buffer[l]) * 0x7FFF;
    }
    return btoa(String.fromCharCode.apply(null, new Uint8Array(buf.buffer)));
  }

  constructor(private router: Router) { }

  startConnection() {
    this.hubConnection =
      new signalR.HubConnectionBuilder()
        .withUrl("https://localhost:44389/stream")
        .build();

    this.hubConnection
      .start()
      .then(() => console.log("Hub connection started"))
      .catch(err => console.log('Error while starting hub connection: ' + err));
  }

  startVoiceStream() {
    this.hubConnection.send("StartRecognition")
      .catch(err => console.log("server Recognition() error: " + err));

    navigator.mediaDevices.getUserMedia({ audio: true })
      .then(stream => {
        this.audioCtx = new AudioContext({ sampleRate: 48000 });

        var scriptNode = this.audioCtx.createScriptProcessor(2048, 1, 1);
        scriptNode.onaudioprocess = (audioProcessingEvent) => {
          var inputBuffer = audioProcessingEvent.inputBuffer;

          for (var channel = 0; channel < inputBuffer.numberOfChannels; channel++) {
            var chunk = audioProcessingEvent.inputBuffer.getChannelData(channel);
            this.hubConnection.invoke("ProcessVoiceStream", this.covertFloat32ToUInt8(chunk));
          }
        }
        var source = this.audioCtx.createMediaStreamSource(stream);
        source.connect(scriptNode);
        scriptNode.connect(this.audioCtx.destination);

        this.stream = stream;
      })
      .catch(function (e) {
        console.error('startRecording() error: ' + e.message);
      })
  }
  stopVoiceStream() {
    try {
      let stream = this.stream;
      stream.getAudioTracks().forEach(track => track.stop());
      this.audioCtx.close();
    }
    catch (error) {
      console.error('stopRecording() error: ' + error);
    }
  }

  public stopVoiceStreamListener = () => {
    this.hubConnection.on('InvokeStopVoiceStream', () => {
      this.stopVoiceStream();
    });
  }
  public broadcastMessageRuEngListener = () => {
    this.hubConnection.on("BroadcastMessageRuEng", (message) => {
      this.newMessageReceived.next(message);
    });
  }
  public broadcastVoiceMessageListener = () => {
    this.hubConnection.on("SayVoiceMessage", (message) => {
      this.voiceMessageReceived.next(message);
    });
  }
  public broadcastIntentListener = () => {
    this.hubConnection.on("BroadcastIntent", (intent) => {
      this.newIntentReceived.next(intent);
    });
  }
  public switchFlightDataListener = () => {
    this.hubConnection.on("SwitchToFlightData", () => {
      this.router.navigateByUrl('/flight-data');
    });
  }
  public fetchFlightDataListener = () => {
    this.hubConnection.on("FetchFlightData", (rootObj) => {
      console.log("123")
      this.flightDataFetched.next(rootObj);
    });
  }
}
