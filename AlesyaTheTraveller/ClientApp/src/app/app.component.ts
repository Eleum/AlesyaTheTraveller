import { Component, OnInit } from '@angular/core';
import { SignalRService } from './services/signal-r.service';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit {
  public message = "";
  public intent = "";

  constructor(public signalRService: SignalRService, private http: HttpClient) { }

  ngOnInit() {
    this.signalRService.startConnection();
    this.signalRService.stopVoiceStreamListener();
    this.signalRService.broadcastMessageRuEngListener();
    this.signalRService.broadcastVoiceMessageListener();
    this.signalRService.broadcastIntentListener();

    // subscribe to update UI with message on new messages from server
    this.signalRService.newMessageReceived
      .subscribe((message) => {
        this.message = message;
      });

    // subscribe to update UI with intent on new intents from server
    this.signalRService.newIntentReceived
      .subscribe((intent) => {
        this.intent = intent;
      });

    // subscribe to say new messages
    this.signalRService.voiceMessageReceived
      .subscribe((message) => {
        this.sayVoiceMessageHandler(message);
      });
  }

  startRecording() {
    this.message = "";
    this.signalRService.startVoiceStream();
  }

  stopRecording() {
    this.signalRService.stopVoiceStream();
  }

  private sayVoiceMessageHandler(message: string) {
    var formData = new FormData();
    formData.append('message', `лошара, ты сказал ${message}`);
    this.http.post('https://localhost:44389/api/VoiceStreaming', formData)
      .subscribe((response: any) => {
        let byteCharacters = atob(response.response);
        let byteNumbers = new Array(byteCharacters.length);
        for (var i = 0; i < byteCharacters.length; i++) {
          byteNumbers[i] = byteCharacters.charCodeAt(i);
        }

        let byteArray = new Uint8Array(byteNumbers);
        let blob = new Blob([byteArray], { type: response.contentType });
        let url = URL.createObjectURL(blob);

        let audio = new Audio();
        audio.src = url;
        let playPromise = audio.play();

        // only Chrome(?) supports play promise
        if (playPromise !== undefined) {
          playPromise.then(function () {
            // playback started
          }).catch(function (error) {
            console.error(error);
            // playback failed.
            // do smthing with it
          });
        }
      }, err => {
        console.error(err)
      });
  }

  title = 'app';
}
