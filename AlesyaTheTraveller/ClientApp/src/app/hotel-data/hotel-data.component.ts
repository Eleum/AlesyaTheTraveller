import { Component, OnInit, OnDestroy, AfterViewInit } from '@angular/core';
import { Observable, Subscription } from 'rxjs';
import { SignalRService } from '../services/signal-r.service';
import { HotelData } from './hotel-data.model';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-hotel-data',
  templateUrl: './hotel-data.component.html',
  styleUrls: ['./hotel-data.component.css']
})
export class HotelDataComponent implements OnInit, AfterViewInit, OnDestroy {
  private hotelText: string;
  private hotelData: Observable<HotelData[]>;
  private subscription: Subscription;
  private notifySubscription: Subscription;

  constructor(private signalr: SignalRService, private toastr: ToastrService) { }

  ngOnInit() {
    this.hotelData = this.signalr.hotelDataFetched.asObservable();
    this.subscription = this.hotelData.subscribe(data => {
      if (data != undefined && data.length > 0) {
        this.hotelText = "Вот список мест для проживания. Выбирайте любое."
      }
      console.log(data);
    });

    this.notifySubscription = this.signalr.notifyTriggered.subscribe(notification => {
      this.signalr.notify(this.toastr, notification);
    });
  }

  ngAfterViewInit() {
    setTimeout(() => {
      this.signalr.fetchData(1);
    });
  }

  ngOnDestroy() {
    this.subscription.unsubscribe();
    this.notifySubscription.unsubscribe();
  }

  startRecording() {
    this.signalr.startRecording();
  }

  getHotelClass(hotel: HotelData) {
    return new Array(hotel.Class);
  }
}
