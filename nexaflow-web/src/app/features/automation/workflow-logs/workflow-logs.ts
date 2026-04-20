import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { AutomationService } from '../services/automation.service';
import { WorkflowLogDto } from '../../../shared/models/automation.models';

/** Paged execution history for a single rule. */
@Component({
  selector: 'app-workflow-logs',
  imports: [DatePipe, RouterLink, MatButtonModule, MatIconModule, MatProgressBarModule],
  template: `
    <div class="page-header">
      <button mat-icon-button routerLink="/automation" aria-label="Back"><mat-icon>arrow_back</mat-icon></button>
      <h1>Execution Logs</h1>
    </div>

    @if (loading()) { <mat-progress-bar mode="indeterminate" /> }

    @if (!loading() && logs().length === 0) {
      <div class="empty-state">
        <mat-icon>receipt_long</mat-icon>
        <p>No executions yet. Run the rule's “Test” to generate one.</p>
      </div>
    } @else {
      <div class="logs">
        @for (log of logs(); track log.id) {
          <div class="log" [class]="log.status.toLowerCase()">
            <div class="log-head">
              <span class="status">{{ statusIcon(log) }} {{ log.status }}</span>
              <span class="when">{{ log.executedAt | date: 'medium' }}</span>
            </div>
            @if (log.triggerData) { <p class="trigger">{{ log.triggerData }}</p> }
            <pre class="details">{{ log.details }}</pre>
          </div>
        }
      </div>

      <div class="pager">
        <button mat-stroked-button [disabled]="page() === 1" (click)="go(page() - 1)">Previous</button>
        <span>Page {{ page() }} of {{ totalPages() }} ({{ total() }} total)</span>
        <button mat-stroked-button [disabled]="page() >= totalPages()" (click)="go(page() + 1)">Next</button>
      </div>
    }
  `,
  styles: `
    .page-header { display: flex; align-items: center; gap: 8px; padding: 24px 24px 8px; }
    .logs { padding: 16px 24px; display: flex; flex-direction: column; gap: 12px; }
    .log { padding: 12px 16px; border: 1px solid var(--mat-sys-outline-variant); border-left-width: 4px; border-radius: 8px; background: var(--mat-sys-surface-container-low); }
    .log.success { border-left-color: var(--mat-sys-tertiary); }
    .log.partialsuccess { border-left-color: #f0ad4e; }
    .log.failed { border-left-color: var(--mat-sys-error); }
    .log-head { display: flex; justify-content: space-between; font-size: 13px; }
    .status { font-weight: 600; }
    .when { color: var(--mat-sys-on-surface-variant); }
    .trigger { margin: 6px 0 4px; font-size: 13px; color: var(--mat-sys-on-surface-variant); }
    .details { margin: 4px 0 0; white-space: pre-wrap; font-family: inherit; font-size: 13px; }
    .pager { display: flex; justify-content: center; align-items: center; gap: 16px; padding: 16px; }
    .empty-state { display: grid; place-items: center; gap: 8px; padding: 64px; color: var(--mat-sys-on-surface-variant); }
  `,
})
export class WorkflowLogs {
  private readonly automation = inject(AutomationService);
  private readonly route = inject(ActivatedRoute);

  private readonly ruleId = this.route.snapshot.paramMap.get('id')!;
  private readonly pageSize = 10;

  readonly logs = signal<WorkflowLogDto[]>([]);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly loading = signal(false);

  constructor() {
    this.go(1);
  }

  totalPages(): number {
    return Math.max(1, Math.ceil(this.total() / this.pageSize));
  }

  statusIcon(log: WorkflowLogDto): string {
    return log.status === 'Success' ? '✅' : log.status === 'Failed' ? '❌' : '⚠️';
  }

  go(page: number): void {
    this.loading.set(true);
    this.automation.getLogs(this.ruleId, page, this.pageSize).subscribe({
      next: (res) => {
        this.logs.set(res.items);
        this.total.set(res.total);
        this.page.set(res.page);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}
