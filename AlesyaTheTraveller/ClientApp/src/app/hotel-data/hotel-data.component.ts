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

  constructor(private service: SignalRService, private toastr: ToastrService) { }

  ngOnInit() {
    this.hotelData = this.service.hotelDataFetched.asObservable();
    this.subscription = this.hotelData.subscribe(data => {
      if (data != undefined && data.length > 0) {
        this.hotelText = "Вот список мест для проживания. Выбирайте любое."
      }
      console.log(data);
    });
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

  ngAfterViewInit() {
    setTimeout(() => {
      this.service.fetchData(1);
    });
  }

  ngOnDestroy() {
    this.subscription.unsubscribe();
  }

  startRecording() {
    this.service.startRecording();
  }

  getHotelClass(hotel: HotelData) {
    return new Array(hotel.Class);
  }
}
