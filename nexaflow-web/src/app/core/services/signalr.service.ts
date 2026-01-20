import { Injectable, inject, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';
import { NotificationService } from './notification.service';
import { WorkflowNotification } from '../../shared/models/automation.models';

/**
 * Maintains the SignalR connection to the API's notification hub. Surfaces workflow-execution
 * events both as a signal (for reactive UI) and as a toast. Connection is opened once from the
 * shell after login and torn down on logout.
 */
@Injectable({ providedIn: 'root' })
export class SignalRService {
  private readonly auth = inject(AuthService);
  private readonly notifications = inject(NotificationService);

  private connection?: signalR.HubConnection;

  /** Last workflow execution pushed from the server, or null before any has arrived. */
  readonly latestWorkflow = signal<WorkflowNotification | null>(null);

  connect(): void {
    if (this.connection) return; // already connected

    // Hub lives at the API root (strip the trailing /api from the base url).
    const hubUrl = `${environment.apiBaseUrl.replace(/\/api$/, '')}/hubs/notifications`;

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, { accessTokenFactory: () => this.auth.accessToken ?? '' })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    this.connection.on('WorkflowExecuted', (data: WorkflowNotification) => {
      this.latestWorkflow.set(data);
      const verb = data.status === 'Failed' ? 'failed' : 'ran';
      this.notifications.info(`Automation "${data.ruleName}" ${verb}: ${data.summary}`);
    });

    this.connection.start().catch((err) => console.error('SignalR connection error:', err));
  }

  disconnect(): void {
    this.connection?.stop();
    this.connection = undefined;
  }
}
