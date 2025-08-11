import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { environment } from '../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class Signalr 
{
  private hubConnection!: signalR.HubConnection;
  public messages: { user: string; text: string }[] = [];

  // Bắt đầu kết nối
  public startConnection() 
  {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.apiUrl}/chathub`) // URL của hub bên backend
      .withAutomaticReconnect()
      .build();

    // Lắng nghe sự kiện từ server
    this.hubConnection.on('ReceiveMessage', (user: string, text: string) => 
    {
      console.log('uswe', user)
      this.messages.push({ user, text });
    });

    // Kết nối
    this.hubConnection.start()
      .then(() => console.log('Connected to SignalR'))
      .catch(err => console.error('SignalR connection error: ', err));
  }

  // Gửi tin nhắn lên server
  public sendMessage(user: string, text: string) 
  {
    if (this.hubConnection) 
      {
      this.hubConnection.invoke('SendMessage', user, text)
        .catch(err => console.error(err));
    }
  }
}
