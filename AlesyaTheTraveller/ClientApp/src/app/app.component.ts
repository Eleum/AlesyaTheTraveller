import { Component, OnInit } from '@angular/core';
import { SignalRService } from './services/signal-r.service';
import { HttpClient, HttpResponse } from '@angular/common/http';
import { Response } from '@angular/http';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit {
  public message = "";

  constructor(public signalRService: SignalRService, private http: HttpClient) { }

  ngOnInit() {
    this.signalRService.startConnection();
    this.signalRService.stopVoiceStreamListener();
    this.signalRService.broadcastMessageRuEngListener();

    this.signalRService.newMessageAdded
      .subscribe((message) => {
        this.message = message;
      });

    this.signalRService.sayVoiceMessage
      .subscribe((message) => {

      });
  }

  startRecording() {
    this.message = "";
    this.signalRService.startVoiceStream();
  }

  stopRecording() {
    this.signalRService.stopVoiceStream();
  }

  async fetchAudio(message: string) {
    var formData = new FormData();
    formData.append('message', 'приветь');

    const requestOptions = {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: formData
    };
  }

  sayVoiceMessage(message: string) {
    var formData = new FormData();
    formData.append('message', 'приветь');
    this.http.post('https://localhost:44389/api/VoiceStreaming', formData, { observe: 'response', responseType: 'blob' })
      .subscribe((response: HttpResponse<any>) => {
        debugger;
        console.log(response.body);
        let headers = Array.from(response.body.content.headers);
        let fileSize = parseInt(headers.filter((header: any) => { return header.key === 'Content-Length' })[0]['value'][0]);
        let disposition = headers.filter((header: any) => { return header.key === 'Content-Disposition' })[0]['value'][0];

        if (disposition && disposition.indexOf('attachment') !== -1) {
          let fileName = "";
          let filenameRegex = /filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/;
          let matches = filenameRegex.exec(disposition);

          if (matches != null && matches[1]) {
            fileName = matches[1].replace(/['"]/g, '');
          } else {
            console.error("Response disposition is not valid: " + disposition)
          }
        }


      }, err => {
        console.log(err)
      });
  }

  title = 'app';
}
