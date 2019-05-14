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

  constructor(private service: SignalRService, private toastr: ToastrService) { }

  ngOnInit() {
    // subscribe to update UI with message on new messages from server [TEST]
    this.service.newMessageReceived
      .subscribe((message) => {
        this.message = message;
      });

    // subscribe to update UI with intent on new intents from server [TEST]
    this.service.newIntentReceived
      .subscribe((intent) => {
        this.intent = intent;
      });

    this.toastr.toastrConfig.progressBar = true;
    this.toastr.toastrConfig.tapToDismiss
    this.toastr.toastrConfig.progressAnimation = 'decreasing';
    this.service.notifyTriggered
      .subscribe(notification => {
        switch (notification.type) {
          case "1":
            this.toastr.info(notification.message);
            break;
          case "2":
            this.toastr.warning(notification.message);
            break;
          case "3":
            this.toastr.error(notification.message);
            break;
        }
      });
  }

  startRecording() {
    this.service.startRecording();
  }

  fillInput(message: string) {
    this.input = message;
  }
}
