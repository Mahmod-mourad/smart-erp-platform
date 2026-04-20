import { Component, input } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ChatMessage } from '../../../shared/models/chat.models';

/** Dumb presentational bubble for a single chat message, aligned by role. */
@Component({
  selector: 'app-chat-bubble',
  imports: [DatePipe],
  template: `
    <div class="bubble" [class.user]="message().role === 'user'">
      <span class="text">{{ message().content }}</span>
      <span class="time">{{ message().timestamp | date: 'shortTime' }}</span>
    </div>
  `,
  styles: `
    :host { display: block; }
    .bubble {
      max-width: 80%; margin: 6px 0; padding: 8px 12px; border-radius: 12px;
      background: var(--mat-sys-surface-container-highest); color: var(--mat-sys-on-surface);
      display: flex; flex-direction: column; gap: 2px; white-space: pre-wrap; word-break: break-word;
    }
    .bubble.user {
      margin-left: auto; background: var(--mat-sys-primary); color: var(--mat-sys-on-primary);
    }
    .time { font-size: 10px; opacity: .7; align-self: flex-end; }
  `,
})
export class ChatBubble {
  readonly message = input.required<ChatMessage>();
}
