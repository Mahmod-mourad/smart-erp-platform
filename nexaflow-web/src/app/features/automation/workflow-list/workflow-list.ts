import { Component, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { AutomationService } from '../services/automation.service';
import { NotificationService } from '../../../core/services/notification.service';
import {
  ConfirmDialog,
  ConfirmDialogData,
} from '../../../shared/confirm-dialog/confirm-dialog';
import { TRIGGER_LABELS, WorkflowRuleDto } from '../../../shared/models/automation.models';

/** Smart list of automation rules with inline toggle, test, logs and delete. */
@Component({
  selector: 'app-workflow-list',
  imports: [
    DatePipe, RouterLink, MatButtonModule, MatIconModule,
    MatProgressBarModule, MatSlideToggleModule, MatDialogModule,
  ],
  template: `
    <div class="page-header">
      <h1>Automation</h1>
      <button mat-flat-button color="primary" routerLink="/automation/new">
        <mat-icon>add</mat-icon> New Rule
      </button>
    </div>

    @if (automation.isLoading()) { <mat-progress-bar mode="indeterminate" /> }

    @if (!automation.isLoading() && automation.rules().length === 0) {
      <div class="empty-state">
        <mat-icon>bolt</mat-icon>
        <p>No automation rules yet. Create one to put the busywork on autopilot.</p>
        <button mat-stroked-button routerLink="/automation/new">Create your first rule</button>
      </div>
    } @else {
      <div class="grid">
        @for (rule of automation.rules(); track rule.id) {
          <div class="card" [class.inactive]="!rule.isActive">
            <div class="card-top">
              <span class="badge trigger">{{ triggerLabel(rule) }}</span>
              <mat-slide-toggle
                [checked]="rule.isActive"
                (change)="toggle(rule)"
                aria-label="Activate or deactivate rule"
              />
            </div>

            <h3>{{ rule.name }}</h3>
            <p class="muted">{{ rule.description || 'No description' }}</p>

            <div class="stats">
              <span><mat-icon>play_circle</mat-icon> {{ ruleActionCount(rule) }} action(s)</span>
              <span>
                <mat-icon>history</mat-icon>
                {{ rule.lastExecutedAt ? (rule.lastExecutedAt | date: 'short') : 'Never run' }}
              </span>
              <span><mat-icon>check_circle</mat-icon>
                {{ rule.successfulExecutions }}/{{ rule.totalExecutions }} ok</span>
            </div>

            <div class="actions">
              <button mat-stroked-button (click)="test(rule)">
                <mat-icon>flash_on</mat-icon> Test
              </button>
              <button mat-stroked-button [routerLink]="['/automation', rule.id, 'logs']">
                <mat-icon>receipt_long</mat-icon> Logs
              </button>
              <button mat-icon-button color="warn" (click)="remove(rule)" aria-label="Delete rule">
                <mat-icon>delete</mat-icon>
              </button>
            </div>
          </div>
        }
      </div>
    }
  `,
  styles: `
    .page-header { display: flex; justify-content: space-between; align-items: center; padding: 24px 24px 8px; }
    .grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(300px, 1fr)); gap: 16px; padding: 16px 24px; }
    .card {
      padding: 16px; border: 1px solid var(--mat-sys-outline-variant);
      border-radius: 12px; background: var(--mat-sys-surface-container-low);
    }
    .card.inactive { opacity: .6; }
    .card-top { display: flex; justify-content: space-between; align-items: center; margin-bottom: 8px; }
    .card h3 { margin: 0 0 4px; }
    .muted { color: var(--mat-sys-on-surface-variant); margin: 0 0 12px; font-size: 13px; }
    .badge { font-size: 11px; padding: 3px 10px; border-radius: 999px; }
    .badge.trigger { background: var(--mat-sys-secondary-container); color: var(--mat-sys-on-secondary-container); }
    .stats { display: flex; flex-direction: column; gap: 4px; font-size: 13px; color: var(--mat-sys-on-surface-variant); margin-bottom: 12px; }
    .stats span { display: inline-flex; align-items: center; gap: 6px; }
    .stats mat-icon { font-size: 16px; width: 16px; height: 16px; }
    .actions { display: flex; gap: 8px; align-items: center; }
    .actions button:last-child { margin-left: auto; }
    .empty-state { display: grid; place-items: center; gap: 12px; padding: 64px; color: var(--mat-sys-on-surface-variant); }
  `,
})
export class WorkflowList {
  protected readonly automation = inject(AutomationService);
  private readonly dialog = inject(MatDialog);
  private readonly notify = inject(NotificationService);

  constructor() {
    this.automation.loadAll().subscribe({ error: () => {} });
  }

  triggerLabel(rule: WorkflowRuleDto): string {
    return TRIGGER_LABELS[rule.triggerType] ?? rule.triggerType;
  }

  ruleActionCount(rule: WorkflowRuleDto): number {
    try {
      const parsed = JSON.parse(rule.actionsConfig);
      return Array.isArray(parsed) ? parsed.length : 0;
    } catch {
      return 0;
    }
  }

  toggle(rule: WorkflowRuleDto): void {
    this.automation.toggle(rule.id).subscribe({
      next: (r) => this.notify.success(`Rule ${r.isActive ? 'activated' : 'deactivated'}.`),
      error: () => {},
    });
  }

  test(rule: WorkflowRuleDto): void {
    this.automation.test(rule.id).subscribe({
      next: () => this.notify.info('Rule executed — check the logs.'),
      error: () => {},
    });
  }

  remove(rule: WorkflowRuleDto): void {
    const data: ConfirmDialogData = {
      title: 'Delete rule',
      message: `Delete "${rule.name}"? This also removes its execution history.`,
      confirmText: 'Delete',
      destructive: true,
    };
    this.dialog
      .open(ConfirmDialog, { data })
      .afterClosed()
      .subscribe((confirmed?: boolean) => {
        if (confirmed) {
          this.automation.remove(rule.id).subscribe({
            next: () => this.notify.success('Rule deleted.'),
            error: () => {},
          });
        }
      });
  }
}
