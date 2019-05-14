import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Subject } from 'rxjs';
import * as signalR from '@aspnet/signalr';
import { FlightData } from '../flight-data/flight-data.model';
import { HotelData } from '../hotel-data/hotel-data.model';
import { debug } from 'util';

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
  //public voiceMessageReceived = new Subject<string>();
  public newIntentReceived = new Subject<string>();
  public flightDataFetched = new Subject<FlightData[]>();
  public hotelDataFetched = new Subject<HotelData[]>();
  public sortingCalled = new Subject<number>();
  public notifyTriggered = new Subject<any>();

  covertFloat32ToUInt8(buffer: Float32Array) {
    let l = buffer.length;
    let buf = new Int16Array(l);
    while (l--) {
      buf[l] = Math.min(1, buffer[l]) * 0x7FFF;
    }
    return btoa(String.fromCharCode.apply(null, new Uint8Array(buf.buffer)));
  }

  constructor(private router: Router, private http: HttpClient) {
    this.startConnection();
    this.stopVoiceStreamListener();
    this.broadcastMessageRuEngListener();
    this.broadcastVoiceMessageListener();
    this.broadcastIntentListener();
    this.switchItemListener();
    this.fetchDataListener();
    this.sortDataListener();
  }

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

  startRecording() {
    this.startVoiceStream();
  }

  stopRecording() {
    this.stopVoiceStream();
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
      this.audioCtx.close().catch(err => {
        debugger;
        console.error(err)
      });
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

  private sayVoiceMessageHandler(message: string) {
    var formData = new FormData();
    formData.append('message', message);
    this.http.post('https://localhost:44389/api/VoiceStreaming', formData)
      .subscribe((response: any) => {
        let byteCharacters = atob(response.response);
        let byteNumbers = new Array(byteCharacters.length);
        for (var i = 0; i < byteCharacters.length; i++) {
          byteNumbers[i] = byteCharacters.charCodeAt(i);
        }

        let byteArray = new Uint8Array(byteNumbers);
        let blob = new Blob([byteArray], { type: response.contentType });
        let url = URL.createObjectURL(blob);

        let audio = new Audio();
        audio.src = url;
        let playPromise = audio.play();

        // only Chrome(?) supports play promise
        if (playPromise !== undefined) {
          playPromise.then(function () {
            // playback started
          }).catch(function (error) {
            console.error(error);
            // playback failed.
            // do smthing with it
          });
        }
      }, err => {
        console.error(err)
      });
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
      debugger;
      this.sayVoiceMessageHandler(message);
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
            this.sayVoiceMessageHandler("Отели по запросу не найдены");
          } else {
            this.isHotelsVoiceLineSaid = true;
            this.sayVoiceMessageHandler(`"Вот где можно остановиться в городе ${this.destinationCity}"`);
          }
        }
      } else if (item.startsWith("main")) {
        this.router.navigateByUrl('/');
      }
    });
  }

  public fetchDataListener = () => {
    this.hubConnection.on("FetchData", (rootObj, type) => {
      this.storedFlightData = null;
      this.storedHotelData = null;

      if (type == 0) {
        debugger;
        if (rootObj == null || rootObj.length == 0) {
          this.sayVoiceMessageHandler("К сожалению, не удалось получить рейсы по заданному направлению. Повторите попытку позже.");
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
        this.sayVoiceMessageHandler("Вот, какие рейсы удалось получить");
      } else {
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
      }
    });
  }

  public sortDataListener = () => {
    this.hubConnection.on("SortData", (type: number) => {
      console.log("sort");
      this.sortData(type);
      this.fetchData(1);
    });
  }

  public notifyListener = () => {
    this.hubConnection.on("Notify", (message, type) => {
      this.notifyTriggered.next({ "message": message, "type": type });
    });
  }

  private sortData(type) {
    if (this.router.url == "/flight-data") {
      console.log(this.storedFlightData);
      if (this.storedFlightData == undefined) {
        return;
      }
      //switch (type) {
      //  case "1": {
      //    this.storedHotelData = this.storedHotelData.sort(function (obj1, obj2) {
      //      return obj1.TotalPrice - obj2.TotalPrice;
      //    });
      //    break;
      //  }
      //  case "2": {
      //    this.storedHotelData = this.storedHotelData.sort(function (obj1, obj2) {
      //      return obj2.TotalPrice - obj1.TotalPrice;
      //    });
      //    break;
      //  }
      //}
    } else if (this.router.url == "/hotel-data") {
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
        case "3": {
          this.storedHotelData = this.storedHotelData.sort(function (obj1, obj2) {
            return obj2.ReviewScore - obj1.ReviewScore;
          });
          break;
        }
        case "4": {
          this.storedHotelData = this.storedHotelData.sort(function (obj1, obj2) {
            return obj1.Class - obj2.Class;
          });
          break;
        }
        case "5": {
          this.storedHotelData = this.storedHotelData.sort(function (obj1, obj2) {
            return obj2.Class - obj1.Class;
          });
          break;
        }
        case "6": {
          this.storedHotelData = this.storedHotelData.sort(function (obj1, obj2) {
            return obj2.ReviewsCount - obj1.ReviewsCount;
          });
          break;
        }
      }
    }
    
  }
}
