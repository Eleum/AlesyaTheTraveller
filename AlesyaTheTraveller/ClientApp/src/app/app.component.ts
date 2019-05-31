import { Component } from '@angular/core';
import { SignalRService } from './services/signal-r.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent {

  constructor(private signalr: SignalRService) { }

  fillInput(message: string) {
    this.signalr.sendMessage(message);
  }

  deb() {
    debugger;
  }

  title = 'app';
}
