import { Component, OnInit } from '@angular/core';
import { Signalr } from '../signalr';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-chat',
  imports: [FormsModule, CommonModule],
  templateUrl: './chat.html',
  styleUrl: './chat.scss'
})
export class Chat implements OnInit
{
  user = '';
  message = '';

  constructor(public signalRService: Signalr) {}
  ngOnInit(): void 
  {
    this.signalRService.startConnection();
  }

  sendMessage() 
  {
    if (this.user && this.message) 
    {
      this.signalRService.sendMessage(this.user, this.message);
      this.message = '';
    }
  }

}
