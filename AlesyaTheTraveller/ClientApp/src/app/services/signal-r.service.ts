import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { Subject } from 'rxjs';
import * as signalR from '@aspnet/signalr';
import { FlightData, HotelData } from '../flight-data/flight-data.model';

@Injectable({
  providedIn: 'root'
})
export class SignalRService {
  private hubConnection: signalR.HubConnection
  private stream: MediaStream;
  private audioCtx: AudioContext;
  private storedFlightData: FlightData[];
  private storedHotelData: HotelData[];

  public newMessageReceived = new Subject<string>();
  public voiceMessageReceived = new Subject<string>();
  public newIntentReceived = new Subject<string>();
  public flightDataFetched = new Subject<FlightData[]>();
  public hotelDataFetched = new Subject<HotelData[]>();

  covertFloat32ToUInt8(buffer: Float32Array) {
    let l = buffer.length;
    let buf = new Int16Array(l);
    while (l--) {
      buf[l] = Math.min(1, buffer[l]) * 0x7FFF;
    }
    return btoa(String.fromCharCode.apply(null, new Uint8Array(buf.buffer)));
  }

  constructor(private router: Router) { }

  startConnection() {
    this.hubConnection =
      new signalR.HubConnectionBuilder()
        .withUrl("https://localhost:44389/stream")
        .build();

    this.hubConnection
      .start()
      .then(() => console.log("Hub connection started"))
      .catch(err => console.log('Error while starting hub connection: ' + err));
  }

  startVoiceStream() {
    this.hubConnection.send("StartRecognition")
      .catch(err => console.log("server Recognition() error: " + err));

    navigator.mediaDevices.getUserMedia({ audio: true })
      .then(stream => {
        this.audioCtx = new AudioContext({ sampleRate: 48000 });

        var scriptNode = this.audioCtx.createScriptProcessor(2048, 1, 1);
        scriptNode.onaudioprocess = (audioProcessingEvent) => {
          var inputBuffer = audioProcessingEvent.inputBuffer;

          for (var channel = 0; channel < inputBuffer.numberOfChannels; channel++) {
            var chunk = audioProcessingEvent.inputBuffer.getChannelData(channel);
            this.hubConnection.invoke("ProcessVoiceStream", this.covertFloat32ToUInt8(chunk));
          }
        }
        var source = this.audioCtx.createMediaStreamSource(stream);
        source.connect(scriptNode);
        scriptNode.connect(this.audioCtx.destination);

        this.stream = stream;
      })
      .catch(function (e) {
        console.error('startRecording() error: ' + e.message);
      })
  }
  stopVoiceStream() {
    try {
      let stream = this.stream;
      stream.getAudioTracks().forEach(track => track.stop());
      this.audioCtx.close();
    }
    catch (error) {
      console.error('stopRecording() error: ' + error);
    }
  }

  fetchData(type: number) {
    console.log("fetch with type" + type);
    if (type == 0) {
      this.flightDataFetched.next(this.storedFlightData);
    } else {
      this.hotelDataFetched.next(this.storedHotelData);
    }
  }

  public stopVoiceStreamListener = () => {
    this.hubConnection.on('InvokeStopVoiceStream', () => {
      this.stopVoiceStream();
    });
  }
  public broadcastMessageRuEngListener = () => {
    this.hubConnection.on("BroadcastMessageRuEng", (message) => {
      this.newMessageReceived.next(message);
    });
  }
  public broadcastVoiceMessageListener = () => {
    this.hubConnection.on("SayVoiceMessage", (message) => {
      this.voiceMessageReceived.next(message);
    });
  }
  public broadcastIntentListener = () => {
    this.hubConnection.on("BroadcastIntent", (intent) => {
      this.newIntentReceived.next(intent);
    });
  }
  public switchFlightDataListener = () => {
    this.hubConnection.on("SwitchToFlightData", () => {
      this.router.navigateByUrl('/flight-data');
    });
  }
  public fetchDataListener = () => {
    this.hubConnection.on("FetchData", (rootObj, type) => {
      if (type == 0) {
        if (rootObj == null)
          return;
        this.storedFlightData = rootObj.map(x => {
          return <FlightData>
            {
              ImageUri: x.carrierImageUri,
              Departure: x.departureTime,
              Arrival: x.arrivalTime,
              Origin: x.origin,
              Destination: x.destination,
              Cost: x.cost,
              Stops: x.stops,
              TicketSellerUri: x.ticketSellerUri
            };
        });
        this.fetchData(0);
      } else {
        console.log("PREdata is " + rootObj);
        if (rootObj == null)
          return;
        this.storedHotelData = rootObj.map(x => {
          return <HotelData>
            {
              Id: x.hotel_id,
              Country: x.country_trans,
              City: x.city_trans,
              OriginAddress: x.address,
              Address: x.address_trans,
              OriginName: x.hotel_name,
              Name: x.hotel_name_trans,
              ImageUri: x.main_photo_url,
              Class: x.class,
              IsFreeCancellation: x.is_free_cancellable,
              IsNoPrepayment: x.is_no_prepayment_block,
              IsSoldOut: x.soldout,
              CurrencyCode: x.currencycode,
              TotalPrice: x.min_total_price,
              ReviewScore: x.review_score,
              ReviewScoreWord: x.review_score_word,
              ReviewsCount: x.review_nr
            };
        });
        console.log("data is " + this.storedHotelData);
        this.fetchData(1);
      }
    });
  }
}
