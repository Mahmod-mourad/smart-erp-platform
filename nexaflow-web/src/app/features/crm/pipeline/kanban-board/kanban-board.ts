import { Component, effect, inject, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import {
  CdkDragDrop,
  DragDropModule,
  moveItemInArray,
  transferArrayItem,
} from '@angular/cdk/drag-drop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { LeadsService } from '../../services/leads.service';
import { CustomersService } from '../../services/customers.service';
import { CrmState } from '../../../../core/state/crm.state';
import { LeadFormDialog } from '../lead-form-dialog/lead-form-dialog';
import { CreateLeadDto, LeadDto, LeadStage } from '../../../../shared/models/crm.models';

interface Column {
  stage: LeadStage;
  leads: LeadDto[];
}

/** Step 7: drag-and-drop sales pipeline. Dropping a card across columns persists the new stage. */
@Component({
  selector: 'app-kanban-board',
  imports: [CurrencyPipe, DragDropModule, MatButtonModule, MatIconModule, MatDialogModule],
  template: `
    <div class="page-header">
      <h1>Sales Pipeline</h1>
      <button mat-flat-button color="primary" (click)="openCreate()">
        <mat-icon>add</mat-icon> New Lead
      </button>
    </div>

    <div class="board" cdkDropListGroup>
      @for (col of board(); track col.stage) {
        <div class="column">
          <header>
            <span class="title">{{ col.stage }}</span>
            <span class="count">{{ col.leads.length }}</span>
            <span class="total">{{ columnTotal(col) | currency: 'EGP' : 'symbol' : '1.0-0' }}</span>
          </header>

          <div
            class="cards"
            cdkDropList
            [cdkDropListData]="col.leads"
            (cdkDropListDropped)="drop($event, col.stage)"
          >
            @for (lead of col.leads; track lead.id) {
              <article class="card" cdkDrag>
                <div class="card-preview" *cdkDragPreview>{{ lead.title }}</div>
                <p class="lead-title">{{ lead.title }}</p>
                <p class="lead-customer">{{ lead.customerName }}</p>
                <p class="lead-value">{{ lead.value | currency: 'EGP' }}</p>
              </article>
            } @empty {
              <p class="empty">Drop leads here</p>
            }
          </div>
        </div>
      }
    </div>
  `,
  styles: `
    :host { display: block; padding: 24px; }
    .page-header { display: flex; justify-content: space-between; align-items: center; }
    .page-header h1 { margin: 0; font: var(--mat-sys-headline-small); }
    .board {
      display: grid;
      grid-auto-flow: column;
      grid-auto-columns: minmax(240px, 1fr);
      gap: 16px;
      margin-top: 16px;
      overflow-x: auto;
      padding-bottom: 8px;
    }
    .column {
      background: var(--mat-sys-surface-container-low);
      border: 1px solid var(--mat-sys-outline-variant);
      border-radius: 12px;
      display: flex;
      flex-direction: column;
      min-height: 200px;
    }
    .column header {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 12px 16px;
      border-bottom: 1px solid var(--mat-sys-outline-variant);
    }
    .column .title { font: var(--mat-sys-title-small); }
    .column .count {
      font: var(--mat-sys-label-small);
      background: var(--mat-sys-secondary-container);
      color: var(--mat-sys-on-secondary-container);
      border-radius: 999px;
      padding: 0 8px;
    }
    .column .total { margin-left: auto; font: var(--mat-sys-body-small); color: var(--mat-sys-on-surface-variant); }
    .cards { flex: 1; padding: 8px; display: flex; flex-direction: column; gap: 8px; min-height: 80px; }
    .card {
      background: var(--mat-sys-surface);
      border: 1px solid var(--mat-sys-outline-variant);
      border-radius: 8px;
      padding: 12px;
      cursor: grab;
      box-shadow: var(--mat-sys-level1);
    }
    .card:active { cursor: grabbing; }
    .card-preview { padding: 12px; background: var(--mat-sys-surface-container-highest); border-radius: 8px; }
    .lead-title { margin: 0; font: var(--mat-sys-title-small); }
    .lead-customer { margin: 2px 0; font: var(--mat-sys-body-small); color: var(--mat-sys-on-surface-variant); }
    .lead-value { margin: 0; font: var(--mat-sys-label-large); }
    .empty { color: var(--mat-sys-on-surface-variant); font: var(--mat-sys-body-small); text-align: center; padding: 16px 0; }
    .cdk-drag-placeholder { opacity: 0.4; }
    .cdk-drop-list-dragging .card:not(.cdk-drag-placeholder) { transition: transform .2s ease; }
  `,
})
export class KanbanBoard {
  private readonly leadsService = inject(LeadsService);
  private readonly customersService = inject(CustomersService);
  private readonly crmState = inject(CrmState);
  private readonly dialog = inject(MatDialog);

  /** Pipeline order, left to right. */
  private static readonly STAGES: LeadStage[] = [
    'Prospect', 'Qualified', 'Proposal', 'Negotiation', 'Won', 'Lost',
  ];

  /**
   * Local, mutable board derived from the shared lead state. CDK splices these arrays in
   * place during a drag, so they cannot be `computed`; an effect rebuilds them whenever the
   * authoritative lead list changes (initial load, create, or a persisted stage move).
   */
  protected readonly board = signal<Column[]>([]);

  constructor() {
    effect(() => {
      const leads = this.crmState.leads();
      this.board.set(
        KanbanBoard.STAGES.map((stage) => ({
          stage,
          leads: leads.filter((l) => l.stage === stage),
        })),
      );
    });

    this.leadsService.loadAll().subscribe({ error: () => {} });
    if (this.crmState.customers().length === 0) {
      this.customersService.loadAll().subscribe({ error: () => {} });
    }
  }

  columnTotal(col: Column): number {
    return col.leads.reduce((sum, l) => sum + l.value, 0);
  }

  drop(event: CdkDragDrop<LeadDto[]>, targetStage: LeadStage): void {
    if (event.previousContainer === event.container) {
      // Reordering within a column is presentational only — no stage change to persist.
      moveItemInArray(event.container.data, event.previousIndex, event.currentIndex);
      return;
    }

    transferArrayItem(
      event.previousContainer.data,
      event.container.data,
      event.previousIndex,
      event.currentIndex,
    );

    const lead = event.container.data[event.currentIndex];
    this.leadsService.updateStage(lead.id, targetStage).subscribe({
      // On failure, move the card back so the board matches the server.
      error: () =>
        transferArrayItem(
          event.container.data,
          event.previousContainer.data,
          event.currentIndex,
          event.previousIndex,
        ),
    });
  }

  openCreate(): void {
    this.dialog
      .open(LeadFormDialog)
      .afterClosed()
      .subscribe((result?: CreateLeadDto) => {
        if (!result) return;
        // Persist, then refresh the shared lead list so the new card lands in its column.
        this.leadsService.create(result).subscribe(() =>
          this.leadsService.loadAll().subscribe({ error: () => {} }),
        );
      });
  }
}
