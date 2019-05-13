import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { Subject } from 'rxjs';
import * as signalR from '@aspnet/signalr';
import { FlightData } from '../flight-data/flight-data.model';
import { HotelData } from '../hotel-data/hotel-data.model';

@Injectable({
  providedIn: 'root'
})
export class SignalRService {
  private hubConnection: signalR.HubConnection
  private stream: MediaStream;
  private audioCtx: AudioContext;
  private storedFlightData: FlightData[];
  private storedHotelData: HotelData[];

  private isHotelsVoiceLineSaid = false;
  private destinationCity: string;

  public newMessageReceived = new Subject<string>();
  public voiceMessageReceived = new Subject<string>();
  public newIntentReceived = new Subject<string>();
  public flightDataFetched = new Subject<FlightData[]>();
  public hotelDataFetched = new Subject<HotelData[]>();
  public sortingCalled = new Subject<number>();

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

  public switchItemListener = () => {
    this.hubConnection.on("SwitchToItem", (data) => {
      let item = data.toLowerCase();
      if (item.startsWith("flight") || item.startsWith("ticket")) {
        this.router.navigateByUrl('/flight-data');
      } else if (item.startsWith("hotel")) {
        this.router.navigateByUrl('/hotel-data');
        if (!this.isHotelsVoiceLineSaid) {
          if (this.storedHotelData == undefined || this.storedHotelData.length == 0) {
            this.voiceMessageReceived.next("Отели по запросу не найдены");
          } else {
            this.isHotelsVoiceLineSaid = true;
            this.voiceMessageReceived.next(`"Вот где можно остановиться в городе ${this.destinationCity}"`);
          }
        }
      } else if (item.startsWith("main")) {
        this.router.navigateByUrl('/');
      }
    });
  }

  public fetchDataListener = () => {
    this.hubConnection.on("FetchData", (rootObj, type) => {
      console.log("fetch data signalr");
      if (type == 0) {
        if (rootObj == null || rootObj.length == 0) {
          this.voiceMessageReceived.next("К сожалению, не удалось получить рейсы по заданному направлению. Повторите попытку позже.");
          return;
        }
        this.isHotelsVoiceLineSaid = false;
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
        this.voiceMessageReceived.next("Вот, какие рейсы удалось получить");
      } else {
        if (rootObj == undefined) {
          console.log("!UNDEFINED");
        }

        if (rootObj === null) {
          console.log("!NULL");
        }
        if (rootObj == null || rootObj.length == 0)
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
        this.destinationCity = this.storedHotelData[0].City;
        this.fetchData(1);
        if (this.storedFlightData != undefined) {
          this.voiceMessageReceived.next("Я еще нашла отели, могу показать");
        }
      }
    });
  }

  public sortDataListener = () => {
    this.hubConnection.on("SortData", (type: number) => {
      console.log("sort");
      // получить активную страницу и вызвать fetchData()
      this.sortData(type);
      this.fetchData(1);
    });
  }

  private sortData(type) {
    console.log(this.storedHotelData);
    if (this.storedHotelData == undefined) {
      return;
    }
    switch (type) {
      case "1": {
        this.storedHotelData = this.storedHotelData.sort(function (obj1, obj2) {
          return obj1.TotalPrice - obj2.TotalPrice;
        });
        break;
      }
      case "2": {
        this.storedHotelData = this.storedHotelData.sort(function (obj1, obj2) {
          return obj2.TotalPrice - obj1.TotalPrice;
        });
        break;
      }
    }
  }
}
