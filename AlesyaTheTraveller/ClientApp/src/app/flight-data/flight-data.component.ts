import { Component, OnInit } from '@angular/core';
import { SignalRService } from '../services/signal-r.service';
import { FlightData } from './flight-data.model';

@Component({
  selector: 'app-flight-data',
  templateUrl: './flight-data.component.html',
  styleUrls: ['./flight-data.component.css']
})
export class FlightDataComponent implements OnInit {
  private rootObj: FlightData;

  constructor(private service: SignalRService) { }

  ngOnInit() {
    this.service.fetchFlightDataListener();

    this.service.flightDataFetched
      .subscribe((rootObj) => {
        debugger;
        this.rootObj = rootObj;
        console.log(this.rootObj);
      });
  }
}
