import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Subject } from 'rxjs';
import * as signalR from '@aspnet/signalr';
import { FlightData } from '../flight-data/flight-data.model';
import { HotelData } from '../hotel-data/hotel-data.model';
import { ToastrService, GlobalConfig } from 'ngx-toastr';

@Injectable({
  providedIn: 'root'
})
export class SignalRService {
  private hubConnection: signalR.HubConnection;
  private audio: HTMLAudioElement;
  private stream: MediaStream;
  private audioCtx: AudioContext;
  private storedFlightData: FlightData[];
  private storedHotelData: HotelData[];
  private toastrConfig: GlobalConfig;

  private messageEnglish: string;


  private isFlightsVoiceLineSaid = false;
  private isNegativeFlightsVoiceLine = false;
  private isHotelsVoiceLineSaid = false;
  private isNegativeHotelsVoiceLine = false;
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
    const buf = new Int16Array(l);
    while (l--) {
      buf[l] = Math.min(1, buffer[l]) * 0x7FFF;
    }
    return btoa(String.fromCharCode.apply(null, new Uint8Array(buf.buffer)));
  }

  constructor(private router: Router, private http: HttpClient, private toastr: ToastrService) {
    this.startConnection();
    this.stopVoiceStreamListener();
    this.broadcastMessageRuEngListener();
    this.broadcastVoiceMessageListener();
    this.broadcastIntentListener();
    this.switchItemListener();
    this.fetchDataListener();
    this.sortDataListener();
    this.notifyListener();
    this.initializeToastrConfig();
    console.log(this.storedFlightData);
    console.log(this.storedHotelData);
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

  private startVoiceStream() {
    this.hubConnection.send("StartRecognition")
      .catch(err => console.log("server Recognition() error: " + err));

    if (this.audio != undefined) {
      this.audio.pause();
      this.audio.currentTime = 0;
    }

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

  private stopVoiceStream() {
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

  private sayVoiceMessageHandler(initialMessage: string, substitution: string = "") {
    var formData = new FormData();
    formData.append('initialMessage', initialMessage);
    formData.append('substitution', substitution);
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

        this.audio = new Audio();
        this.audio.src = url;
        let playPromise = this.audio.play();

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
        console.error(err);
      });
  }

  public stopVoiceStreamListener = () => {
    this.hubConnection.on('InvokeStopVoiceStream', () => {
      this.stopVoiceStream();
    });
  }

  public broadcastMessageRuEngListener = () => {
    this.hubConnection.on("BroadcastMessageRuEng", (message: string) => {
      this.messageEnglish = message.split("|")[1];
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
        if (this.router.url == '/flight-data') {
          this.fetchData(0);
        }
        this.router.navigateByUrl('/flight-data');
        if (this.isNegativeFlightsVoiceLine) {
          this.sayVoiceMessageHandler("К сожалению, не удалось получить рейсы по заданному направлению. Повторите попытку позже.");
          this.isNegativeFlightsVoiceLine = false;
        } else if (!this.isFlightsVoiceLineSaid && (this.storedFlightData != undefined && this.storedFlightData.length > 0)) {
          this.isFlightsVoiceLineSaid = true;
          this.sayVoiceMessageHandler(this.messageEnglish);
        }
      } else if (item.startsWith("hotel")) {
        if (this.router.url == '/hotel-data') {
          this.fetchData(1);
        }
        this.router.navigateByUrl('/hotel-data');
        if (this.isNegativeHotelsVoiceLine) {
          this.sayVoiceMessageHandler("К сожалению, не удалось получить отели по заданному направлению. Повторите попытку позже.");
          this.isNegativeHotelsVoiceLine = false;
        } else if (!this.isHotelsVoiceLineSaid && (this.storedHotelData != undefined && this.storedHotelData.length > 0)) {
          this.isHotelsVoiceLineSaid = true;
          this.sayVoiceMessageHandler(this.messageEnglish, `[ГОРОД]|${this.destinationCity};[ОТЕЛЬ]|${this.storedHotelData[0].Name};[ОТЕЛЬ]|${this.storedHotelData[0].OriginName}`);
        }
      } else if (item.startsWith("main")) {
        this.router.navigateByUrl('/');
      }
    });
  }

  public fetchDataListener = () => {
    this.hubConnection.on("FetchData", (rootObj, type, updateParams) => {
      if (updateParams.update) {
        this.resetVoiceLines(updateParams);
        if (updateParams.updateFlights)
          this.storedFlightData = null;
        if (updateParams.updateHotels)
          this.storedHotelData = null;
      }

      if (rootObj == null || rootObj.length == 0) {
        if (type == 0) {
          this.isNegativeFlightsVoiceLine = true;
        } else {
          this.isNegativeHotelsVoiceLine = true;
        }
        return;
      }

      if (type == 0) {
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
      } else {
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
      console.log("NOTIFY WITH: " + message);
      this.notifyTriggered.next({ "message": message, "type": type });
    });
  }

  public notify(toastr: ToastrService, notification: any) {
    switch (notification.type) {
      case 0:
        // remove all notifications except for success one
        toastr.toastrConfig.maxOpened = 1;
        toastr.success(notification.message, "", { timeOut: 1500 });
        toastr.toastrConfig.maxOpened = 6;
        break;
      case 1:
        toastr.info(notification.message, "", { timeOut: 3000 });
        break;
      case 2:
        toastr.warning(notification.message);
        break;
      case 3:
        toastr.error(notification.message);
        break;
    }
  }

  private resetVoiceLines(updateParams: any) {
    if (updateParams.updateFlights) {
      this.isFlightsVoiceLineSaid = false;
      this.isNegativeFlightsVoiceLine = false;
    } else if (updateParams.updateHotels) {
      this.isHotelsVoiceLineSaid = false;
      this.isNegativeHotelsVoiceLine = false;
    }
  }

  private initializeToastrConfig() {
    this.toastrConfig = this.toastr.toastrConfig;
    this.toastrConfig.autoDismiss = true;
    this.toastrConfig.closeButton = false;
    this.toastrConfig.progressBar = true;
    this.toastrConfig.resetTimeoutOnDuplicate = true;
    this.toastrConfig.maxOpened = 6;
    this.toastrConfig.progressAnimation = 'decreasing';
  }

  public getToastrConfig() {
    return this.toastrConfig;
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

  public sendMessage(mes: string) {
    this.hubConnection.invoke("StartRecognition1", mes);
  }
}
