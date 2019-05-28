import { Component, OnInit, OnDestroy, AfterViewInit } from '@angular/core';
import { SignalRService } from '../services/signal-r.service';
import { Observable, Subscription } from 'rxjs';
import { FlightData } from './flight-data.model';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-flight-data',
  templateUrl: './flight-data.component.html',
  styleUrls: ['./flight-data.component.css']
})
export class FlightDataComponent implements OnInit, AfterViewInit, OnDestroy {
  private flightText: string;
  private flightData: Observable<FlightData[]>;
  private subscription: Subscription;
  private notifySubscription: Subscription;

  constructor(private signalr: SignalRService, private toastr: ToastrService) { }

  ngOnInit() {
    this.toastr.toastrConfig = this.signalr.getToastrConfig();
    this.flightData = this.signalr.flightDataFetched.asObservable();
    this.subscription = this.flightData.subscribe(data => {
      if (data != undefined) {
        this.flightText = "Вот какие есть рейсы по выбранному Вами направлению";
      }
      console.log(data);
    });

    this.notifySubscription = this.signalr.notifyTriggered.subscribe(notification => {
      this.signalr.notify(this.toastr, notification);
    });
  }

  ngAfterViewInit() {
    setTimeout(() => {
      debugger;
      this.signalr.fetchData(0);
    });
  }

  ngOnDestroy() {
    this.subscription.unsubscribe();
    this.notifySubscription.unsubscribe();
  }

  startRecording() {
    this.signalr.startRecording();
  }

  fetch() {
    this.signalr.fetchData(0);
  }
}
