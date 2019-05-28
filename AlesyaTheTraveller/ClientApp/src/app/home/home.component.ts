import { Component, OnInit, OnDestroy } from '@angular/core';
import { SignalRService } from '../services/signal-r.service';
import { ToastrService } from 'ngx-toastr';
import { Subscription } from 'rxjs';


@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
})
export class HomeComponent implements OnInit, OnDestroy {
  private input = "";
  private message = "";
  private intent = "";
  private notifySubscription: Subscription;

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

    this.notifySubscription = this.signalr.notifyTriggered.subscribe(notification => {
      this.signalr.notify(this.toastr, notification);
    });
  }

  ngOnDestroy() {
    this.notifySubscription.unsubscribe();
  }


  startRecording() {
    this.signalr.startRecording();
  }

  fillInput(message: string) {
    this.input = message;
  }
}
