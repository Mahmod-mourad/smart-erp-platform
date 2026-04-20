import {
  Component,
  ElementRef,
  effect,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { ChatbotService } from '../services/chatbot.service';
import { ChatBubble } from '../chat-bubble/chat-bubble';
import { ChatMessage } from '../../../shared/models/chat.models';

const GREETING: ChatMessage = {
  role: 'assistant',
  content: "Hi! I'm your NexaFlow AI assistant. Ask me about your company's data.",
  timestamp: new Date(),
};

/** Floating AI assistant: a FAB that opens a chat panel grounded in the tenant's business data. */
@Component({
  selector: 'app-chat-widget',
  imports: [ChatBubble],
  template: `
    <button class="chat-fab" (click)="toggle()" aria-label="Toggle AI assistant">
      {{ isOpen() ? '✕' : '🤖' }}
    </button>

    @if (isOpen()) {
      <div class="chat-panel">
        <div class="chat-header">
          <span>🤖 NexaFlow AI</span>
          <button class="close" (click)="toggle()" aria-label="Close">×</button>
        </div>

        <div #scroll class="chat-messages">
          @for (msg of messages(); track $index) {
            <app-chat-bubble [message]="msg" />
          }
          @if (isLoading()) {
            <div class="typing"><span></span><span></span><span></span></div>
          }
        </div>

        <div class="chat-input">
          <textarea
            #input
            rows="1"
            placeholder="Ask a question…"
            [value]="inputText()"
            (input)="inputText.set($any($event.target).value)"
            (keydown)="onKey($event)"
          ></textarea>
          <button
            class="send"
            [disabled]="!inputText().trim() || isLoading()"
            (click)="send()"
          >
            Send
          </button>
        </div>
      </div>
    }
  `,
  styles: `
    .chat-fab {
      position: fixed; bottom: 24px; right: 24px; width: 56px; height: 56px; border-radius: 50%;
      background: var(--mat-sys-primary); color: var(--mat-sys-on-primary); border: none;
      font-size: 22px; cursor: pointer; box-shadow: 0 4px 20px rgba(0,0,0,.2); z-index: 1000;
      transition: transform .15s;
    }
    .chat-fab:hover { transform: scale(1.08); }
    .chat-panel {
      position: fixed; bottom: 92px; right: 24px; width: 360px; height: 500px; z-index: 1000;
      display: flex; flex-direction: column; overflow: hidden; border-radius: 16px;
      background: var(--mat-sys-surface); border: 1px solid var(--mat-sys-outline-variant);
      box-shadow: 0 8px 40px rgba(0,0,0,.18);
    }
    .chat-header {
      display: flex; align-items: center; justify-content: space-between; padding: 12px 16px;
      background: var(--mat-sys-primary); color: var(--mat-sys-on-primary); font-weight: 600;
    }
    .chat-header .close { background: none; border: none; color: inherit; font-size: 20px; cursor: pointer; }
    .chat-messages { flex: 1; overflow-y: auto; padding: 12px 16px; }
    .chat-input { display: flex; gap: 8px; padding: 10px; border-top: 1px solid var(--mat-sys-outline-variant); }
    .chat-input textarea {
      flex: 1; resize: none; border-radius: 10px; padding: 8px 10px; font: inherit;
      border: 1px solid var(--mat-sys-outline-variant); background: var(--mat-sys-surface-container-low);
      color: var(--mat-sys-on-surface);
    }
    .chat-input .send {
      border: none; border-radius: 10px; padding: 0 16px; cursor: pointer;
      background: var(--mat-sys-primary); color: var(--mat-sys-on-primary);
    }
    .chat-input .send:disabled { opacity: .5; cursor: default; }
    .typing { display: flex; gap: 4px; padding: 8px 4px; }
    .typing span {
      width: 8px; height: 8px; border-radius: 50%; background: var(--mat-sys-outline);
      animation: typing 1s infinite;
    }
    .typing span:nth-child(2) { animation-delay: .2s; }
    .typing span:nth-child(3) { animation-delay: .4s; }
    @keyframes typing { 0%,100% { transform: translateY(0); } 50% { transform: translateY(-6px); } }
  `,
})
export class ChatWidget {
  private readonly chatbot = inject(ChatbotService);
  private readonly scrollPane = viewChild<ElementRef<HTMLDivElement>>('scroll');
  private readonly inputBox = viewChild<ElementRef<HTMLTextAreaElement>>('input');

  protected readonly isOpen = signal(false);
  protected readonly isLoading = signal(false);
  protected readonly inputText = signal('');
  protected readonly messages = signal<ChatMessage[]>([GREETING]);

  constructor() {
    // Keep the conversation scrolled to the latest message whenever it changes.
    effect(() => {
      this.messages();
      this.isLoading();
      const pane = this.scrollPane()?.nativeElement;
      if (pane) queueMicrotask(() => pane.scrollTo({ top: pane.scrollHeight, behavior: 'smooth' }));
    });
  }

  toggle(): void {
    this.isOpen.update((v) => !v);
    if (this.isOpen()) queueMicrotask(() => this.inputBox()?.nativeElement.focus());
  }

  onKey(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.send();
    }
  }

  send(): void {
    const text = this.inputText().trim();
    if (!text || this.isLoading()) return;

    const history = this.messages();
    this.messages.update((list) => [...list, { role: 'user', content: text, timestamp: new Date() }]);
    this.inputText.set('');
    this.isLoading.set(true);

    this.chatbot.send(text, history).subscribe({
      next: (res) =>
        this.messages.update((list) => [
          ...list,
          { role: 'assistant', content: res.message, timestamp: new Date(res.timestamp) },
        ]),
      error: () =>
        this.messages.update((list) => [
          ...list,
          { role: 'assistant', content: 'Sorry, something went wrong. Please try again.', timestamp: new Date() },
        ]),
      complete: () => this.isLoading.set(false),
    });
  }
}
