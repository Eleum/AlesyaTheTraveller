import { Injectable } from '@angular/core';
import * as signalR from '@aspnet/signalr';
import { Script } from 'vm';

@Injectable({
  providedIn: 'root'
})
export class SignalRService {
  private hubConnection: signalR.HubConnection

  private audioCtx: AudioContext;
  private stream: MediaStream;

  covertFloat32ToInt16(buffer: Float32Array) {
    let l = buffer.length;
    let buf = new Int16Array(l);
    while (l--) {
      buf[l] = Math.min(1, buffer[l]) * 0x7FFF;
    }
    return buf.buffer;
  }

  startRecording() {
    navigator.mediaDevices.getUserMedia({ audio: true })
      .then(stream => {
        this.audioCtx = new AudioContext();
        this.audioCtx.createMediaStreamSource(stream);
        this.audioCtx.onstatechange = (state) => { console.log(state) }

        var scriptNode = this.audioCtx.createScriptProcessor(4096, 1, 1);
        scriptNode.onaudioprocess = (audioProcessingEvent) => {
          var inputBuffer = audioProcessingEvent.inputBuffer;
          for (var channel = 0; channel < inputBuffer.numberOfChannels; channel++) {
            //console.log("inputBuffer: " + audioProcessingEvent.inputBuffer.getChannelData(channel));
            var chunk = audioProcessingEvent.inputBuffer.getChannelData(channel);
            this.hubConnection.invoke("broadcastmessage", this.covertFloat32ToInt16(chunk));
          }
        }
        var source = this.audioCtx.createMediaStreamSource(stream);
        source.connect(scriptNode);
        scriptNode.connect(this.audioCtx.destination);
      })
      .catch(function (e) {
        console.error('startRecording() error: ' + e.message);
      })
  }

  stopRecording() {
    try {
      let stream = this.stream;
      stream.getAudioTracks().forEach(track => track.stop());
      stream.getVideoTracks().forEach(track => track.stop());
      this.audioCtx.close();
    }
    catch (error) {
      console.error('stopRecording() error: ' + error);
    }
  }


  public startConnection = () => {
    this.hubConnection =
      new signalR.HubConnectionBuilder()
        .withUrl("https://localhost:44389/chat")
        .build();

    console.log(this.hubConnection);

    this.hubConnection
      .start()
      .then(() => console.log("Connection started"))
      .catch(err => console.log('Error while starting connection: ' + err))
  }

  public broadcastMessage = (message) => {
    this.hubConnection.invoke('broadcastmessage', message)
      .catch(err => console.error(err));
  }

  public addBroadcastMessageListener = () => {
    this.hubConnection.on('broadcastmessage', (data) => {
      console.log("broadcasted message: " + data);
    })
  }
}
