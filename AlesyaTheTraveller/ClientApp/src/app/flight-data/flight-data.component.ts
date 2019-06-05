import { Component, OnInit, OnDestroy, AfterViewInit } from '@angular/core';
import { SignalRService } from '../services/signal-r.service';
import { Observable, Subscription } from 'rxjs';
import { FlightData } from './flight-data.model';
import { ToastrService } from 'ngx-toastr';
import { PaginatorService } from '../services/paginator.service';

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
  private paginator: any = {};
  private pageItems: any[];

  constructor(private signalr: SignalRService, private toastr: ToastrService, private paginatorService: PaginatorService) { }

  ngOnInit() {
    this.toastr.toastrConfig = this.signalr.getToastrConfig();
    this.flightData = this.signalr.flightDataFetched.asObservable();
    this.subscription = this.flightData.subscribe(
      data => {
        if (data != undefined && data != null) {
          this.flightText = "Вот какие есть рейсы по выбранному Вами направлению";
          this.fd = data;
          this.setPage(1);
        }
        console.log(data);
      },
      (err) => {
        console.error(err);
      }
    );

    this.notifySubscription = this.signalr.notifyTriggered.subscribe(notification => {
      this.signalr.notify(this.toastr, notification);
    });
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
    console.log("flight text - " + this.flightText);
    console.log(this.fd);
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

  setPage(page: number) {
    // get paginator object
    this.paginator = this.paginatorService.getPaginator(this.fd.length, page);

    // get items for specified page
    this.pageItems = this.fd.slice(this.paginator.startIndex, this.paginator.endIndex + 1);
  }

  getPaginator() {
    console.log(this.paginator);
  }
}
