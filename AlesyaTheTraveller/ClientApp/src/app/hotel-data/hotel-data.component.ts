import { Component, OnInit, OnDestroy, AfterViewInit } from '@angular/core';
import { Observable, Subscription } from 'rxjs';
import { SignalRService } from '../services/signal-r.service';
import { HotelData } from '../flight-data/flight-data.model';

@Component({
  selector: 'app-hotel-data',
  templateUrl: './hotel-data.component.html',
  styleUrls: ['./hotel-data.component.css']
})
export class HotelDataComponent implements OnInit, AfterViewInit, OnDestroy {
  private hotelData: Observable<HotelData>;
  private subscription: Subscription;

  constructor(private service: SignalRService) { }

  ngOnInit() {
    this.hotelData = this.service.hotelDataFetched.asObservable();
    this.subscription = this.hotelData.subscribe(data => console.log(data));
  }

  ngAfterViewInit() {
    this.service.fetchData(1);
  }

  ngOnDestroy() {
    this.subscription.unsubscribe();
  }
}
