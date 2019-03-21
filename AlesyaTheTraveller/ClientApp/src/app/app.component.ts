import { Component, OnInit } from '@angular/core';
import { SignalRService } from './services/signal-r.service';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit {

  constructor(public signalRService: SignalRService, private http: HttpClient) { }

  

  ngOnInit() {
    this.signalRService.startConnection();
  }

  startRecording() {
    this.signalRService.startRecording();
  }

  stopRecording() {
    this.signalRService.stopRecording();
  }

  test() {
    this.signalRService.test();
  }

  //public sendMessage = (message) => {
  //  this.signalRService.broadcastMessage(message);
  //}

  title = 'app';
}
