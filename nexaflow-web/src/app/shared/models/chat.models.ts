export type ChatRole = 'user' | 'assistant';

export interface ChatMessage {
  role: ChatRole;
  content: string;
  timestamp: Date;
}

export interface ChatResponse {
  message: string;
  timestamp: string;
  success: boolean;
  errorMessage?: string;
}
