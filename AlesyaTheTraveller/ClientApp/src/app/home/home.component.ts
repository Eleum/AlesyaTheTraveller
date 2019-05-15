import { Component, OnInit } from '@angular/core';
import { SignalRService } from '../services/signal-r.service';
import { ToastrService, GlobalConfig } from 'ngx-toastr';


@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
})
export class HomeComponent implements OnInit {
  private input = "";
  private message = "";
  private intent = "";

  constructor(private signalr: SignalRService, private toastr: ToastrService) { }

  ngOnInit() {
    // subscribe to update UI with message on new messages from server [TEST]
    this.signalr.newMessageReceived
      .subscribe((message) => {
        this.message = message;
      });

    // subscribe to update UI with intent on new intents from server [TEST]
    this.signalr.newIntentReceived
      .subscribe((intent) => {
        this.intent = intent;
      });

    this.signalr.notifyTriggered
      .subscribe(notification => {
        this.signalr.notify(this.toastr, notification);
      });
  }

  startRecording() {
    this.signalr.startRecording();
  }

  fillInput(message: string) {
    this.input = message;
  }
}
