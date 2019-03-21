import { Injectable } from '@angular/core';
import * as signalR from '@aspnet/signalr';

@Injectable({
  providedIn: 'root'
})
export class SignalRService {
  private hubConnection: signalR.HubConnection
  private audioCtx: AudioContext;
  private stream: MediaStream;
  private __awaiter: any;

  base64ArrayBuffer(arrayBuffer) {
    var base64 = ''
    var encodings = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/'

    var bytes = new Uint8Array(arrayBuffer)
    var byteLength = bytes.byteLength
    var byteRemainder = byteLength % 3
    var mainLength = byteLength - byteRemainder

    var a, b, c, d
    var chunk

    // Main loop deals with bytes in chunks of 3
    for (var i = 0; i < mainLength; i = i + 3) {
      // Combine the three bytes into a single integer
      chunk = (bytes[i] << 16) | (bytes[i + 1] << 8) | bytes[i + 2]

      // Use bitmasks to extract 6-bit segments from the triplet
      a = (chunk & 16515072) >> 18 // 16515072 = (2^6 - 1) << 18
      b = (chunk & 258048) >> 12 // 258048   = (2^6 - 1) << 12
      c = (chunk & 4032) >> 6 // 4032     = (2^6 - 1) << 6
      d = chunk & 63               // 63       = 2^6 - 1

      // Convert the raw binary segments to the appropriate ASCII encoding
      base64 += encodings[a] + encodings[b] + encodings[c] + encodings[d]
    }

    // Deal with the remaining bytes and padding
    if (byteRemainder == 1) {
      chunk = bytes[mainLength]

      a = (chunk & 252) >> 2 // 252 = (2^6 - 1) << 2

      // Set the 4 least significant bits to zero
      b = (chunk & 3) << 4 // 3   = 2^2 - 1

      base64 += encodings[a] + encodings[b] + '=='
    } else if (byteRemainder == 2) {
      chunk = (bytes[mainLength] << 8) | bytes[mainLength + 1]
      a = (chunk & 64512) >> 10 // 64512 = (2^6 - 1) << 10
      b = (chunk & 1008) >> 4 // 1008  = (2^6 - 1) << 4
      // Set the 2 least significant bits to zero
      c = (chunk & 15) << 2 // 15    = 2^4 - 1
      base64 += encodings[a] + encodings[b] + encodings[c] + '='
    }
    return base64;
  }

  checkEndian(buffer) {
    var uint8Array = new Uint8Array(buffer);
    var uint16array = new Uint16Array(buffer);
    uint8Array[0] = 0xAA; // set first byte
    uint8Array[1] = 0xBB; // set second byte
    if (uint16array[0] === 0xBBAA) return "little endian";
    if (uint16array[0] === 0xAABB) return "big endian";
    else throw new Error("Something crazy just happened");
  }

  covertFloat32ToUInt8(buffer: Float32Array) {
    let l = buffer.length;
    let buf = new Int16Array(l);
    while (l--) {
      buf[l] = Math.min(1, buffer[l]) * 0x7FFF;
    }

    console.log(this.checkEndian(buf.buffer))

    return btoa(String.fromCharCode.apply(null, new Uint8Array(buf.buffer)));
    //return this.base64ArrayBuffer(buf.buffer);
  }

  public startConnection = () => {

    this.hubConnection =
      new signalR.HubConnectionBuilder()
        .withUrl("https://localhost:44389/stream")
        .build();

    this.hubConnection
      .start()
      .then(() => console.log("Connection started"))
      .catch(err => console.log('Error while starting connection: ' + err));

    // idk wtf is this
    //this.__awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    //  return new (P || (P = Promise))(function (resolve, reject) {
    //    function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
    //    function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
    //    function step(result) { result.done ? resolve(result.value) : new P(function (resolve) { resolve(result.value); }).then(fulfilled, rejected); }
    //    step((generator = generator.apply(thisArg, _arguments || [])).next());
    //  });
    //};

    //document.getElementById("streamButton").addEventListener("click", (event) => this.__awaiter(this, void 0, void 0, function* () {
    //  try {
    //    this.hubConnection.stream("DelayCounter", 500)
    //      .subscribe({
    //        next: (item) => {
    //          var li = document.createElement("li");
    //          li.textContent = item;
    //          document.getElementById("messagesList").appendChild(li);
    //        },
    //        complete: () => {
    //          var li = document.createElement("li");
    //          li.textContent = "Stream completed";
    //          document.getElementById("messagesList").appendChild(li);
    //        },
    //        error: (err) => {
    //          var li = document.createElement("li");
    //          li.textContent = err;
    //          document.getElementById("messagesList").appendChild(li);
    //        },
    //      });
    //  }
    //  catch (e) {
    //    console.error(e.toString());
    //  }
    //  event.preventDefault();
    //}));

    //(() => this.__awaiter(this, void 0, void 0, function* () {
    //  try {
    //    yield this.hubConnection
    //      .start()
    //      .then(() => console.log("Connection started"))
    //      .catch(err => console.log('Error while starting connection: ' + err));
    //  }
    //  catch (e) {
    //    console.error(e.toString());
    //  }
    //}))();
  }

  startRecording() {
    this.hubConnection.send("StartRecognition");
    //this.hubConnection.invoke("StartMethod");
    // подумать, как обрабатывать ошибки
    //.then(() => console.log("Recognition started successfully"))
    //.catch(err => console.log("server Recognition() error: " + err));

    navigator.mediaDevices.getUserMedia({ audio: true })
      .then(stream => {
        this.audioCtx = new AudioContext();
        this.audioCtx.createMediaStreamSource(stream);
        this.audioCtx.onstatechange = (state) => { console.log(state) }

        var scriptNode = this.audioCtx.createScriptProcessor(2048, 1, 1); //4096
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

  stopRecording() {
    try {
      let stream = this.stream;
      stream.getAudioTracks().forEach(track => track.stop());
      stream.getVideoTracks().forEach(track => track.stop());
      this.hubConnection.invoke("SetVariableFalse");
      this.audioCtx.close();
    }
    catch (error) {
      console.error('stopRecording() error: ' + error);
    }
  }

  test() {
    this.hubConnection.invoke("Test");
  }

  //public broadcastMessage = (message) => {
  //  this.hubConnection.invoke('broadcastmessage', message)
  //    .catch(err => console.error(err));
  //}

  //public addBroadcastMessageListener = () => {
  //  this.hubConnection.on('broadcastmessage', (data) => {
  //    console.log("broadcasted message: " + data);
  //  })
  //}
}
