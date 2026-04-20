import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { IntegrationsService } from '../services/integrations.service';
import { NotificationService } from '../../../core/services/notification.service';
import {
  INTEGRATION_META,
  INTEGRATION_ORDER,
  IntegrationDto,
  IntegrationType,
} from '../../../shared/models/integration.models';
import {
  IntegrationConfigDialog,
  IntegrationConfigDialogData,
} from '../config-dialog/integration-config-dialog';

/** Settings grid of connectable integrations with configure + test-connection actions. */
@Component({
  selector: 'app-integration-list',
  imports: [
    DatePipe, MatButtonModule, MatIconModule, MatProgressBarModule,
    MatProgressSpinnerModule, MatDialogModule,
  ],
  template: `
    <div class="page-header">
      <h1>Integrations</h1>
      <p class="subtitle">Connect WhatsApp, email, Slack and Google Sheets to power your automations.</p>
    </div>

    @if (integrations.isLoading()) { <mat-progress-bar mode="indeterminate" /> }

    <div class="grid">
      @for (item of ordered(); track item.type) {
        <div class="card">
          <div class="card-top">
            <div class="title">
              <mat-icon>{{ meta(item.type).icon }}</mat-icon>
              <h3>{{ meta(item.type).name }}</h3>
            </div>
            <span class="badge" [class.connected]="item.isEnabled && item.isConfigured">
              {{ item.isEnabled && item.isConfigured ? 'Connected' : 'Not configured' }}
            </span>
          </div>

          <p class="muted">
            @if (item.lastTestedAt) {
              <mat-icon class="dot" [class.ok]="item.lastTestSuccess" [class.bad]="item.lastTestSuccess === false">
                {{ item.lastTestSuccess ? 'check_circle' : 'error' }}
              </mat-icon>
              Last tested {{ item.lastTestedAt | date: 'short' }}
            } @else {
              Never tested
            }
          </p>

          <div class="actions">
            <button mat-stroked-button (click)="configure(item)">
              <mat-icon>settings</mat-icon> Configure
            </button>
            <button
              mat-stroked-button
              [disabled]="!item.isConfigured || isTesting(item.type)"
              (click)="test(item)"
            >
              @if (isTesting(item.type)) {
                <mat-spinner diameter="16" />
              } @else {
                <mat-icon>wifi_tethering</mat-icon>
              }
              Test Connection
            </button>
          </div>
        </div>
      }
    </div>
  `,
  styles: `
    .page-header { padding: 24px 24px 0; }
    .page-header h1 { margin: 0; }
    .subtitle { color: var(--mat-sys-on-surface-variant); margin: 4px 0 0; }
    .grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(300px, 1fr)); gap: 16px; padding: 16px 24px; }
    .card {
      padding: 16px; border: 1px solid var(--mat-sys-outline-variant);
      border-radius: 12px; background: var(--mat-sys-surface-container-low);
    }
    .card-top { display: flex; justify-content: space-between; align-items: flex-start; gap: 8px; margin-bottom: 8px; }
    .title { display: inline-flex; align-items: center; gap: 8px; }
    .title h3 { margin: 0; font-size: 16px; }
    .badge {
      font-size: 11px; padding: 3px 10px; border-radius: 999px;
      background: var(--mat-sys-surface-container-highest); color: var(--mat-sys-on-surface-variant);
    }
    .badge.connected { background: var(--mat-sys-tertiary-container); color: var(--mat-sys-on-tertiary-container); }
    .muted { display: inline-flex; align-items: center; gap: 6px; color: var(--mat-sys-on-surface-variant); font-size: 13px; margin: 0 0 12px; }
    .dot { font-size: 16px; width: 16px; height: 16px; }
    .dot.ok { color: var(--mat-sys-primary); }
    .dot.bad { color: var(--mat-sys-error); }
    .actions { display: flex; gap: 8px; }
    .actions button { display: inline-flex; align-items: center; gap: 6px; }
  `,
})
export class IntegrationList {
  protected readonly integrations = inject(IntegrationsService);
  private readonly dialog = inject(MatDialog);
  private readonly notify = inject(NotificationService);

  private readonly testing = signal<Set<IntegrationType>>(new Set());

  // Render in a stable, designed order regardless of API ordering.
  protected readonly ordered = computed(() => {
    const byType = new Map(this.integrations.integrations().map((i) => [i.type, i]));
    return INTEGRATION_ORDER.map(
      (type) =>
        byType.get(type) ??
        ({ type, isEnabled: false, isConfigured: false } as IntegrationDto),
    );
  });

  constructor() {
    this.integrations.loadAll().subscribe({ error: () => {} });
  }

  protected meta(type: IntegrationType) {
    return INTEGRATION_META[type];
  }

  protected isTesting(type: IntegrationType): boolean {
    return this.testing().has(type);
  }

  configure(item: IntegrationDto): void {
    const data: IntegrationConfigDialogData = {
      type: item.type,
      isEnabled: item.isEnabled,
      isConfigured: item.isConfigured,
    };
    this.dialog
      .open(IntegrationConfigDialog, { data })
      .afterClosed()
      .subscribe((dto) => {
        if (!dto) return;
        this.integrations.upsert(item.type, dto).subscribe({
          next: () => this.notify.success(`${this.meta(item.type).name} saved.`),
          error: () => {},
        });
      });
  }

  test(item: IntegrationDto): void {
    this.setTesting(item.type, true);
    this.integrations.test(item.type).subscribe({
      next: (result) =>
        result.success
          ? this.notify.success(`✅ ${result.message}`)
          : this.notify.error(`❌ ${result.message}`),
      error: () => this.notify.error('Test failed — check your credentials.'),
      complete: () => this.setTesting(item.type, false),
    });
  }

  private setTesting(type: IntegrationType, on: boolean): void {
    this.testing.update((set) => {
      const next = new Set(set);
      if (on) next.add(type);
      else next.delete(type);
      return next;
    });
  }
}
