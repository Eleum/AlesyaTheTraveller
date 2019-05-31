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
  private fd: FlightData[];

  constructor(private signalr: SignalRService, private toastr: ToastrService) { }

  ngOnInit() {
    this.toastr.toastrConfig = this.signalr.getToastrConfig();
    this.flightData = this.signalr.flightDataFetched.asObservable();
    this.subscription = this.flightData.subscribe(
      data => {
        if (data != undefined && data != null) {
          this.flightText = "Вот какие есть рейсы по выбранному Вами направлению";
        }
        console.log(data);
      },
      (err) => {
        debugger;
      }
    );

    this.notifySubscription = this.signalr.notifyTriggered.subscribe(notification => {
      this.signalr.notify(this.toastr, notification);
    });

    var test =
    {
      ImageUri: 'https://s1.apideeplink.com/images/airlines/B2.png',
      Departure: new Date(2019, 5, 1),
      Arrival: new Date(2019, 5, 5),
      Origin: 'МИНСК ИНТЕРНЭШНЛ 2',
      Destination: 'ФРАНКФУРТ-НА-МАЙНЕ',
      Cost: 1545.94,
      Stops: 1,
      TicketSellerUri: ''
    };
    this.fd = [test];
  }

  ngAfterViewInit() {
    setTimeout(() => {
      this.signalr.fetchData(0);
    });

    setTimeout(() => {
      let cardbodyElem = document.querySelectorAll('.card-body');
      let innerDivElem = document.querySelectorAll('.inner-left');

      if (window.innerWidth <= 600) {
        cardbodyElem.forEach(function (elem) {
          elem.classList.add('row');
        });
        innerDivElem.forEach(function (elem) {
          elem.classList.add('col-sm-12');
        });
      } else {
        cardbodyElem.forEach(function (elem) {
          elem.classList.add('row');
        });
        innerDivElem.forEach(function (elem) {
          elem.classList.add('col-sm-12');
        })
      }

      window.onresize = function () {
        if (window.innerWidth <= 600) {
          cardbodyElem.forEach(function (elem) {
            elem.classList.add('row');
          });
          innerDivElem.forEach(function (elem) {
            elem.classList.add('col-sm-12');
          });
        } else {
          cardbodyElem.forEach(function (elem) {
            elem.classList.add('row');
          });
          innerDivElem.forEach(function (elem) {
            elem.classList.add('col-sm-12');
          })
        }
      };
    }, 1000);
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

  resizeHandler(cardbodyElem, innerDivElem) {
    if (window.innerWidth <= 600) {
      cardbodyElem.classList.add('row');
      innerDivElem.classList.add('col-sm-12');
    } else {
      cardbodyElem.classList.remove('row');
      innerDivElem.classList.remove('col-sm-12');
    }
  }
}
