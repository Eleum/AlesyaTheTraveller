import { Component, OnInit, OnDestroy, AfterViewInit, ChangeDetectorRef, AfterContentInit, AfterViewChecked } from '@angular/core';
import { SignalRService } from '../services/signal-r.service';
import { Observable, Subscription } from 'rxjs';
import { FlightData } from './flight-data.model';

@Component({
  selector: 'app-flight-data',
  templateUrl: './flight-data.component.html',
  styleUrls: ['./flight-data.component.css']
})
export class FlightDataComponent implements OnInit, AfterViewInit, OnDestroy {
  private flightData: Observable<FlightData[]>;
  private subscription: Subscription;

  constructor(private service: SignalRService, private cd: ChangeDetectorRef) { }

  ngOnInit() {
    this.flightData = this.service.flightDataFetched.asObservable();
    this.subscription = this.flightData.subscribe(data => {
      console.log(data);
    });
  }

  ngAfterViewInit() {
    setTimeout(() => {
      this.service.fetchData(0);
    });
  }

  ngOnDestroy() {
    this.subscription.unsubscribe();
  }

  fetch() {
    this.service.fetchData(0);
  }
}
