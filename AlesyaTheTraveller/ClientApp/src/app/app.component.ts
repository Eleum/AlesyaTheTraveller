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

      });
  }

  startRecording() {
    this.message = "";
    this.signalRService.startVoiceStream();
  }

  stopRecording() {
    this.signalRService.stopVoiceStream();
  }

  private sayVoiceMessage(message: string) {
    this.http.post('https://localhost:44389/api/VoiceStreaming/', { message: "приветь" });
  }

  title = 'app';
}
