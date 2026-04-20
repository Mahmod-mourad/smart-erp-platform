import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { ChatMessage, ChatResponse } from '../../../shared/models/chat.models';

/** Talks to the AI assistant endpoint, sending the new message plus prior turns for context. */
@Injectable({ providedIn: 'root' })
export class ChatbotService {
  private readonly api = inject(ApiService);

  send(message: string, history: ChatMessage[]): Observable<ChatResponse> {
    return this.api.post<ChatResponse>('chat', {
      message,
      history: history.map((m) => ({ role: m.role, content: m.content })),
    });
  }

  checkStatus(): Observable<{ available: boolean }> {
    return this.api.get<{ available: boolean }>('chat/status');
  }
}
