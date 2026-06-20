import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatTableModule } from '@angular/material/table';
import { LeavesService } from '../../services/leaves.service';
import { LeaveRequestDto } from '../../../../shared/models/hr.models';

/** Manager view: filter leave requests and approve/reject pending ones. */
@Component({
  selector: 'app-leave-list',
  imports: [
    RouterLink, DatePipe, MatButtonModule, MatIconModule, MatButtonToggleModule, MatTableModule,
  ],
  template: `
    <div class="page-header">
      <h1>Leave Requests</h1>
      <a mat-flat-button color="primary" routerLink="apply">
        <mat-icon>add</mat-icon> Apply for Leave
      </a>
    </div>

    <mat-button-toggle-group [value]="filter()" (change)="onFilter($event.value)" class="filters">
      @for (f of filters; track f) { <mat-button-toggle [value]="f">{{ f }}</mat-button-toggle> }
    </mat-button-toggle-group>

    <table mat-table [dataSource]="leaves()" class="table">
      <ng-container matColumnDef="employee">
        <th mat-header-cell *matHeaderCellDef>Employee</th>
        <td mat-cell *matCellDef="let l">{{ l.employeeName }}</td>
      </ng-container>
      <ng-container matColumnDef="dates">
        <th mat-header-cell *matHeaderCellDef>Dates</th>
        <td mat-cell *matCellDef="let l">{{ l.startDate | date: 'mediumDate' }} → {{ l.endDate | date: 'mediumDate' }}</td>
      </ng-container>
      <ng-container matColumnDef="type">
        <th mat-header-cell *matHeaderCellDef>Type</th>
        <td mat-cell *matCellDef="let l">{{ l.type }}</td>
      </ng-container>
      <ng-container matColumnDef="days">
        <th mat-header-cell *matHeaderCellDef>Days</th>
        <td mat-cell *matCellDef="let l">{{ l.totalDays }}</td>
      </ng-container>
      <ng-container matColumnDef="status">
        <th mat-header-cell *matHeaderCellDef>Status</th>
        <td mat-cell *matCellDef="let l"><span class="status" [class]="l.status.toLowerCase()">{{ l.status }}</span></td>
      </ng-container>
      <ng-container matColumnDef="actions">
        <th mat-header-cell *matHeaderCellDef>Actions</th>
        <td mat-cell *matCellDef="let l">
          @if (l.status === 'Pending') {
            <button mat-icon-button color="primary" (click)="approve(l)" title="Approve"><mat-icon>check</mat-icon></button>
            <button mat-icon-button color="warn" (click)="reject(l)" title="Reject"><mat-icon>close</mat-icon></button>
          }
        </td>
      </ng-container>
      <tr mat-header-row *matHeaderRowDef="columns"></tr>
      <tr mat-row *matRowDef="let row; columns: columns"></tr>
    </table>

    @if (leaves().length === 0) {
      <div class="empty-state"><mat-icon>event_busy</mat-icon><p>No leave requests.</p></div>
    }
  `,
  styles: `
    .page-header { display: flex; justify-content: space-between; align-items: center; padding: 24px 24px 8px; }
    .filters { margin: 0 24px 16px; }
    .table { width: calc(100% - 48px); margin: 0 24px; }
    .status { font-size: 12px; padding: 2px 8px; border-radius: 999px; }
    .status.pending { background: var(--mat-sys-secondary-container); color: var(--mat-sys-on-secondary-container); }
    .status.approved { background: var(--mat-sys-tertiary-container); color: var(--mat-sys-on-tertiary-container); }
    .status.rejected { background: var(--mat-sys-error-container); color: var(--mat-sys-on-error-container); }
    .empty-state { display: grid; place-items: center; gap: 8px; padding: 48px; color: var(--mat-sys-on-surface-variant); }
  `,
})
export class LeaveList {
  private readonly leavesService = inject(LeavesService);

  protected readonly filters = ['All', 'Pending', 'Approved', 'Rejected'];
  protected readonly columns = ['employee', 'dates', 'type', 'days', 'status', 'actions'];
  protected readonly filter = signal('Pending');
  protected readonly leaves = signal<LeaveRequestDto[]>([]);

  constructor() {
    this.load();
  }

  private load(): void {
    this.leavesService.getAll(this.filter()).subscribe((data) => this.leaves.set(data));
  }

  onFilter(value: string): void {
    this.filter.set(value);
    this.load();
  }

  approve(leave: LeaveRequestDto): void {
    const note = window.prompt('Approval note (optional):') ?? '';
    this.leavesService.review(leave.id, { approved: true, reviewNote: note }).subscribe(() => this.load());
  }

  reject(leave: LeaveRequestDto): void {
    const note = window.prompt('Rejection reason:');
    if (!note) return;
    this.leavesService.review(leave.id, { approved: false, reviewNote: note }).subscribe(() => this.load());
  }
}
