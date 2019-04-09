import { Component, OnInit } from '@angular/core';
import { SignalRService } from './services/signal-r.service';
import { HttpClient, HttpHeaders, HttpErrorResponse } from '@angular/common/http';
import { throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit {
  public message = "";
  private yandexSpeech = require('yandex-speech');

  constructor(public signalRService: SignalRService, private http: HttpClient) { }

  ngOnInit() {
    this.signalRService.startConnection();
    this.signalRService.stopVoiceStreamListener();
    this.signalRService.broadcastMessageRuEngListener();

    this.signalRService.newMessageAdded
      .subscribe((message) => {
        this.message = message;
      });

    this.signalRService.sayVoiceMessage
      .subscribe((message) => {

        this.SayVoiceMessageHandler(message);



        this.yandexSpeech.TTS({
          developer_key: '3b7b9fba-cbcd-47d1-854a-b359ca0e5da7',
          text: message,
          file: 'alesya_response.mp3'
        });

        debugger;

        var audio = new Audio('alesya_response.mp3');
        audio.play();
      });
  }

  startRecording() {
    this.message = "";
    this.signalRService.startVoiceStream();
  }

  stopRecording() {
    this.signalRService.stopVoiceStream();
  }

  private SayVoiceMessageHandler(message: string) {
    //let iamToken = "CggaATEVAgAAABKABJCOiAl34jOZrOg7139s9VeoBAVho8jB--BipTWiwZY_WXbJE4CzwZpr9XQwkd2h1wLBVyreU1clSWIoThHxAv8JZqsDGqvl-su34y92e7_doarIvwyANTbfYkqanborSkXwNn5QBAm-oxgZVBCg1VtmQFIZKV0Ek9SyVTFjD4sBiTxAD6n1An0d41Z-mjMDaQ-CxQMpswR2-ipPPe_TrAoL6YeHuH5uY_dJ_VxwciLJ52J7QkaLRDXPIWNLwMdSEIqcAXUYL_fQyZsgllPDQ2QxHqx2rn2DezUil3Ecu9WqVvktFyBFJb8k1BIYZbAaSLrTaDuiyn67yUoFBfNnity4yDoBnoiZ4BUn81ssod40doqHXzwpYr541KenKZy1RzelsHG3-xZuXBfZi4fgFGmQ2y_Zx1qBpb64haL5AjODEjgz2GhXBGpGFmLu3bHZKrY2VPw9Essy7OX5N3vVHqPcpd4l1ddtq7fRonjfT7f4ellbacF65OYLQIxzjpXXCDqEn0e90_NbzCYSZowi543z01vXic7wmq35S1nN5MJ3VCPx-ZF4ibFd3Q5ERDpMQ6CmAL5QbrhDYb2pIPp6WZIR1OpGWK3CHvnQZqi_wvx2Kir9s2AFbHXIiiVIfH13eXBlOaUDghgZS5TVSsolEGBRmYXgs5mLQ3p_9ux03yTsGmAKIGM1OTk0ZDg1M2I4ZTQyNDRhYjZjNTUwNTg2NGU4YjgxEJ36s-UFGN3LtuUFIh4KFGFqZWxuYjY2Yjg3cTc5a2Z2Z2xzEgZFLjFldW1aADACOAFKCBoBMRUCAAAAUAEg7wQ";
    //let content = {
    //  text: message,
    //  lang: "ru-RU",
    //  folderId: "b1gfb1uihgi76nm570vu"
    //};
    //let httpOptions = {
    //  headers: new HttpHeaders({
    //    "Authorization": "Bearer " + iamToken
    //  })
    //};

    //var response = this.http
    //  .post("https://tts.api.cloud.yandex.net/speech/v1/tts:synthesize", content, httpOptions)
    //  .pipe(
    //    catchError(this.handleError)
    //);


  }

  //private setAudioElementSource(id: string) {
  //  document.getElementById('voice-response').setAttribute('src', 'http://localhost:8000/api/VoiceStreaming/')
  //}

  title = 'app';
}
