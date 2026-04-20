import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { AutomationService } from '../services/automation.service';
import { NotificationService } from '../../../core/services/notification.service';
import {
  ACTION_LABELS,
  ActionBlock,
  ActionType,
  TRIGGER_LABELS,
  TriggerType,
} from '../../../shared/models/automation.models';

interface FieldDef {
  key: string;
  label: string;
  type: 'number' | 'text' | 'textarea' | 'select';
  options?: { value: string; label: string }[];
  optional?: boolean;
}

const TRIGGER_FORMS: Record<TriggerType, FieldDef[]> = {
  StockLow: [
    { key: 'threshold', label: 'Alert when stock is at/below', type: 'number' },
    { key: 'productId', label: 'Specific product id (blank = all products)', type: 'text', optional: true },
  ],
  EmployeeAbsent: [
    { key: 'checkDeadlineHour', label: 'Check at hour (0–23, Cairo time)', type: 'number' },
  ],
  LeaveRequestPending: [],
  ScheduledDaily: [
    { key: 'hour', label: 'Run at hour (0–23)', type: 'number' },
    { key: 'minute', label: 'Minute (0–59)', type: 'number' },
  ],
  ScheduledWeekly: [
    {
      key: 'dayOfWeek', label: 'Day of week', type: 'select',
      options: ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday']
        .map((d) => ({ value: d, label: d })),
    },
    { key: 'hour', label: 'Run at hour (0–23)', type: 'number' },
    { key: 'minute', label: 'Minute (0–59)', type: 'number' },
  ],
};

const ACTION_FORMS: Record<ActionType, FieldDef[]> = {
  SendEmail: [
    { key: 'to', label: 'Recipient email', type: 'text' },
    { key: 'subject', label: 'Subject', type: 'text' },
    { key: 'body', label: 'Body (supports {{summary}}, {{timestamp}})', type: 'textarea' },
  ],
  SendWhatsApp: [
    { key: 'to', label: 'Phone (+201012345678)', type: 'text' },
    { key: 'message', label: 'Message (supports {{summary}})', type: 'textarea' },
  ],
  SendSlack: [
    { key: 'message', label: 'Message (supports {{summary}})', type: 'textarea' },
    { key: 'channel', label: 'Channel (blank = default)', type: 'text', optional: true },
  ],
  CreateActivity: [
    { key: 'customerId', label: 'Customer id', type: 'text' },
    {
      key: 'activityType', label: 'Activity type', type: 'select',
      options: ['Note', 'Call', 'Email', 'Meeting'].map((t) => ({ value: t, label: t })),
    },
    { key: 'subject', label: 'Subject', type: 'text' },
  ],
};

/** 3-step no-code rule builder: pick a trigger, add actions, review & save. */
@Component({
  selector: 'app-workflow-builder',
  imports: [
    FormsModule, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule, MatSelectModule,
  ],
  template: `
    <div class="page-header">
      <h1>New Automation Rule</h1>
      <button mat-button (click)="cancel()">Cancel</button>
    </div>

    <div class="stepper">
      <span [class.done]="step() !== 'trigger'" [class.current]="step() === 'trigger'">1. Trigger</span>
      <span [class.current]="step() === 'actions'">2. Actions</span>
      <span [class.current]="step() === 'review'">3. Review</span>
    </div>

    <div class="panel">
      @if (step() === 'trigger') {
        <h2>When this happens…</h2>
        <mat-form-field appearance="outline" class="full">
          <mat-label>Trigger</mat-label>
          <mat-select [(ngModel)]="triggerType" (ngModelChange)="onTriggerChange()">
            @for (t of triggerTypes; track t) {
              <mat-option [value]="t">{{ triggerLabel(t) }}</mat-option>
            }
          </mat-select>
        </mat-form-field>

        @for (f of triggerFields(); track f.key) {
          <mat-form-field appearance="outline" class="full">
            <mat-label>{{ f.label }}</mat-label>
            @if (f.type === 'select') {
              <mat-select [(ngModel)]="triggerConfig[f.key]" [name]="f.key">
                @for (o of f.options; track o.value) {
                  <mat-option [value]="o.value">{{ o.label }}</mat-option>
                }
              </mat-select>
            } @else {
              <input matInput [type]="f.type" [(ngModel)]="triggerConfig[f.key]" [name]="f.key" />
            }
          </mat-form-field>
        }

        <div class="nav">
          <span></span>
          <button mat-flat-button color="primary" [disabled]="!triggerType()" (click)="step.set('actions')">
            Next <mat-icon>arrow_forward</mat-icon>
          </button>
        </div>
      }

      @if (step() === 'actions') {
        <h2>…do this</h2>
        @for (action of actions(); track action.id; let i = $index) {
          <div class="action-block">
            <div class="action-head">
              <mat-form-field appearance="outline">
                <mat-label>Action {{ i + 1 }}</mat-label>
                <mat-select [(ngModel)]="action.type" [name]="'type' + action.id" (ngModelChange)="resetActionConfig(action)">
                  @for (a of actionTypes; track a) {
                    <mat-option [value]="a">{{ actionLabel(a) }}</mat-option>
                  }
                </mat-select>
              </mat-form-field>
              <button mat-icon-button color="warn" (click)="removeAction(action.id)" aria-label="Remove action">
                <mat-icon>delete</mat-icon>
              </button>
            </div>

            @for (f of actionFields(action.type); track f.key) {
              <mat-form-field appearance="outline" class="full">
                <mat-label>{{ f.label }}</mat-label>
                @if (f.type === 'select') {
                  <mat-select [(ngModel)]="action.config[f.key]" [name]="f.key + action.id">
                    @for (o of f.options; track o.value) {
                      <mat-option [value]="o.value">{{ o.label }}</mat-option>
                    }
                  </mat-select>
                } @else if (f.type === 'textarea') {
                  <textarea matInput rows="3" [(ngModel)]="action.config[f.key]" [name]="f.key + action.id"></textarea>
                } @else {
                  <input matInput [type]="f.type" [(ngModel)]="action.config[f.key]" [name]="f.key + action.id" />
                }
              </mat-form-field>
            }
          </div>
        }

        <button mat-stroked-button (click)="addAction()">
          <mat-icon>add</mat-icon> Add Action
        </button>

        <div class="nav">
          <button mat-button (click)="step.set('trigger')">
            <mat-icon>arrow_back</mat-icon> Back
          </button>
          <button mat-flat-button color="primary" [disabled]="actions().length === 0" (click)="step.set('review')">
            Next <mat-icon>arrow_forward</mat-icon>
          </button>
        </div>
      }

      @if (step() === 'review') {
        <h2>Review & save</h2>
        <mat-form-field appearance="outline" class="full">
          <mat-label>Rule name</mat-label>
          <input matInput [(ngModel)]="ruleName" name="ruleName" />
        </mat-form-field>
        <mat-form-field appearance="outline" class="full">
          <mat-label>Description (optional)</mat-label>
          <input matInput [(ngModel)]="description" name="description" />
        </mat-form-field>

        <p class="summary">
          <strong>When</strong> {{ triggerLabel(triggerType()!) }}
          <strong>→</strong> run {{ actions().length }} action(s):
          {{ actionSummary() }}
        </p>

        <div class="nav">
          <button mat-button (click)="step.set('actions')">
            <mat-icon>arrow_back</mat-icon> Back
          </button>
          <button mat-flat-button color="primary" [disabled]="!ruleName().trim() || saving()" (click)="save()">
            <mat-icon>save</mat-icon> Save Rule
          </button>
        </div>
      }
    </div>
  `,
  styles: `
    .page-header { display: flex; justify-content: space-between; align-items: center; padding: 24px 24px 8px; }
    .stepper { display: flex; gap: 24px; padding: 0 24px 16px; color: var(--mat-sys-on-surface-variant); }
    .stepper .current { color: var(--mat-sys-primary); font-weight: 600; }
    .stepper .done { color: var(--mat-sys-tertiary); }
    .panel { max-width: 640px; margin: 0 24px; padding: 24px; border: 1px solid var(--mat-sys-outline-variant); border-radius: 12px; background: var(--mat-sys-surface-container-low); }
    .panel h2 { margin-top: 0; }
    .full { width: 100%; }
    .action-block { padding: 12px; margin-bottom: 16px; border: 1px solid var(--mat-sys-outline-variant); border-radius: 10px; }
    .action-head { display: flex; gap: 8px; align-items: center; }
    .action-head mat-form-field { flex: 1; }
    .nav { display: flex; justify-content: space-between; margin-top: 16px; }
    .summary { background: var(--mat-sys-surface-container); padding: 12px; border-radius: 8px; }
  `,
})
export class WorkflowBuilder {
  private readonly automation = inject(AutomationService);
  private readonly notify = inject(NotificationService);
  private readonly router = inject(Router);

  protected readonly triggerTypes = Object.keys(TRIGGER_FORMS) as TriggerType[];
  protected readonly actionTypes = Object.keys(ACTION_FORMS) as ActionType[];

  readonly step = signal<'trigger' | 'actions' | 'review'>('trigger');
  readonly triggerType = signal<TriggerType | null>(null);
  readonly actions = signal<ActionBlock[]>([]);
  readonly ruleName = signal('');
  readonly description = signal('');
  readonly saving = signal(false);

  // Mutated in place by ngModel; read on save.
  protected triggerConfig: Record<string, unknown> = {};

  triggerLabel(t: TriggerType): string { return TRIGGER_LABELS[t]; }
  actionLabel(a: ActionType): string { return ACTION_LABELS[a]; }
  triggerFields(): FieldDef[] { return this.triggerType() ? TRIGGER_FORMS[this.triggerType()!] : []; }
  actionFields(type: ActionType): FieldDef[] { return ACTION_FORMS[type]; }

  onTriggerChange(): void {
    this.triggerConfig = {};
  }

  addAction(): void {
    this.actions.update((list) => [
      ...list,
      { id: crypto.randomUUID(), type: 'SendEmail', config: {} },
    ]);
  }

  removeAction(id: string): void {
    this.actions.update((list) => list.filter((a) => a.id !== id));
  }

  resetActionConfig(action: ActionBlock): void {
    action.config = {};
  }

  actionSummary(): string {
    return this.actions().map((a) => ACTION_LABELS[a.type]).join(', ');
  }

  cancel(): void {
    this.router.navigate(['/automation']);
  }

  save(): void {
    const triggerType = this.triggerType();
    if (!triggerType) return;

    this.saving.set(true);
    const dto = {
      name: this.ruleName().trim(),
      description: this.description().trim() || undefined,
      triggerType,
      triggerConfig: JSON.stringify(this.triggerConfig),
      actionsConfig: JSON.stringify(
        this.actions().map((a) => ({ type: a.type, ...a.config })),
      ),
    };

    this.automation.create(dto).subscribe({
      next: () => {
        this.notify.success('Automation rule created.');
        this.router.navigate(['/automation']);
      },
      error: () => this.saving.set(false),
    });
  }
}
