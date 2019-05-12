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

  constructor(public service: SignalRService, private http: HttpClient) { }

  ngOnInit() {
    this.service.startConnection();
    this.service.stopVoiceStreamListener();
    this.service.broadcastMessageRuEngListener();
    this.service.broadcastVoiceMessageListener();
    this.service.broadcastIntentListener();
    this.service.switchItemListener();
    this.service.fetchDataListener();

    // subscribe to update UI with message on new messages from server
    this.service.newMessageReceived
      .subscribe((message) => {
        this.message = message;
      });

    // subscribe to update UI with intent on new intents from server
    this.service.newIntentReceived
      .subscribe((intent) => {
        this.intent = intent;
      });

    // subscribe to say new messages
    this.service.voiceMessageReceived
      .subscribe((message) => {
        this.sayVoiceMessageHandler(message);
      });
  }

  startRecording() {
    this.message = "";
    this.service.startVoiceStream();
  }

  stopRecording() {
    this.service.stopVoiceStream();
  }

  private sayVoiceMessageHandler(message: string) {
    var formData = new FormData();
    formData.append('message', message);
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
