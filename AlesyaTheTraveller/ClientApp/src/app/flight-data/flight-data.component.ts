import { Component, OnInit, OnDestroy, AfterViewInit, ChangeDetectorRef, AfterContentInit, AfterViewChecked } from '@angular/core';
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

  constructor(private service: SignalRService, private toastr: ToastrService) { }

  ngOnInit() {
    this.service
    this.flightData = this.service.flightDataFetched.asObservable();
    this.subscription = this.flightData.subscribe(data => {
      if (data != undefined) {
        this.flightText = "Вот какие есть рейсы по выбранному Вами направлению";
      }
      console.log(data);
    });

    this.toastr.toastrConfig.progressBar = true;
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

  ngAfterViewInit() {
    setTimeout(() => {
      this.service.fetchData(0);
    });
  }

  ngOnDestroy() {
    this.subscription.unsubscribe();
  }

  startRecording() {
    this.service.startRecording();
  }

  fetch() {
    this.service.fetchData(0);
  }
}
