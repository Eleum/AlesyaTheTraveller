import { Component, OnInit } from '@angular/core';
import { SignalRService } from '../services/signal-r.service';
import { Observable, of } from 'rxjs';
import { FlightData } from './flight-data.model';

@Component({
  selector: 'app-flight-data',
  templateUrl: './flight-data.component.html',
  styleUrls: ['./flight-data.component.css']
})
export class FlightDataComponent implements OnInit {
  private flightData: Observable<FlightData>;

  constructor(private service: SignalRService) { }

  ngOnInit() {
    this.service.fetchFlightDataListener();
    this.flightData = this.service.flightDataFetched.asObservable();
    this.flightData.subscribe(data => console.log(data));
  }
}
