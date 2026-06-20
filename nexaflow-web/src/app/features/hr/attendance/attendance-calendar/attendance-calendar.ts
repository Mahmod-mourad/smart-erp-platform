import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { AttendanceService } from '../../services/attendance.service';
import { AttendanceDto } from '../../../../shared/models/hr.models';

interface CalendarDay {
  date: string; // 'YYYY-MM-DD', or '' for leading blanks
  dayNumber: number;
}

const STATUS_CLASS: Record<string, string> = {
  Present: 'success',
  Absent: 'danger',
  OnLeave: 'info',
  Late: 'warning',
};

const STATUS_ICON: Record<string, string> = {
  Present: '✓',
  Absent: '○',
  OnLeave: 'L',
  Late: '⏰',
};

/** Monthly attendance grid for one employee, with check-in/out for today. */
@Component({
  selector: 'app-attendance-calendar',
  imports: [MatButtonModule, MatIconModule],
  template: `
    <div class="cal-header">
      <button mat-icon-button (click)="prevMonth()"><mat-icon>chevron_left</mat-icon></button>
      <h3>{{ monthLabel() }}</h3>
      <button mat-icon-button (click)="nextMonth()"><mat-icon>chevron_right</mat-icon></button>
      <span class="spacer"></span>
      <button mat-stroked-button (click)="checkIn()">Check in</button>
    </div>

    <div class="weekdays">
      @for (w of weekdays; track w) { <span>{{ w }}</span> }
    </div>

    <div class="grid">
      @for (day of calendarDays(); track $index) {
        @if (day.date) {
          <div class="cell" [class]="statusClass(day.date)"
               [title]="status(day.date)" (click)="onDayClick(day.date)">
            <span class="num">{{ day.dayNumber }}</span>
            <span class="icon">{{ statusIcon(day.date) }}</span>
          </div>
        } @else {
          <div class="cell empty"></div>
        }
      }
    </div>

    <div class="legend">
      <span class="success">✓ Present</span>
      <span class="warning">⏰ Late</span>
      <span class="info">L Leave</span>
      <span class="danger">○ Absent</span>
    </div>
  `,
  styles: `
    .cal-header { display: flex; align-items: center; gap: 8px; }
    .cal-header h3 { margin: 0; min-width: 160px; text-align: center; }
    .spacer { flex: 1; }
    .weekdays, .grid { display: grid; grid-template-columns: repeat(7, 1fr); gap: 6px; }
    .weekdays { margin: 12px 0 6px; font-size: 12px; color: var(--mat-sys-on-surface-variant); text-align: center; }
    .cell {
      aspect-ratio: 1; border-radius: 8px; padding: 6px; cursor: default;
      border: 1px solid var(--mat-sys-outline-variant); display: flex;
      flex-direction: column; justify-content: space-between;
    }
    .cell.empty { border: none; }
    .cell .num { font-size: 13px; font-weight: 500; }
    .cell .icon { font-size: 16px; align-self: flex-end; }
    .cell.success { background: var(--mat-sys-tertiary-container); }
    .cell.warning { background: #fff3cd; }
    .cell.info { background: var(--mat-sys-secondary-container); }
    .cell.danger { background: var(--mat-sys-error-container); }
    .legend { display: flex; gap: 16px; margin-top: 16px; font-size: 12px; flex-wrap: wrap; }
    .legend span { padding: 2px 8px; border-radius: 6px; }
    .legend .success { background: var(--mat-sys-tertiary-container); }
    .legend .warning { background: #fff3cd; }
    .legend .info { background: var(--mat-sys-secondary-container); }
    .legend .danger { background: var(--mat-sys-error-container); }
  `,
})
export class AttendanceCalendar {
  readonly employeeId = input.required<string>();

  private readonly attendanceService = inject(AttendanceService);

  protected readonly weekdays = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
  protected readonly year = signal(new Date().getFullYear());
  protected readonly month = signal(new Date().getMonth() + 1); // 1-based
  protected readonly attendanceMap = signal(new Map<string, AttendanceDto>());

  protected readonly monthLabel = computed(() =>
    new Date(this.year(), this.month() - 1, 1).toLocaleString('en-US', { month: 'long', year: 'numeric' }),
  );

  protected readonly calendarDays = computed<CalendarDay[]>(() => {
    const y = this.year();
    const m = this.month();
    const firstDay = new Date(y, m - 1, 1).getDay();
    const daysInMonth = new Date(y, m, 0).getDate();
    const cells: CalendarDay[] = [];
    for (let i = 0; i < firstDay; i++) cells.push({ date: '', dayNumber: 0 });
    for (let d = 1; d <= daysInMonth; d++) {
      const date = `${y}-${String(m).padStart(2, '0')}-${String(d).padStart(2, '0')}`;
      cells.push({ date, dayNumber: d });
    }
    return cells;
  });

  constructor() {
    // Reload whenever the employee or the visible month changes.
    effect(() => {
      const id = this.employeeId();
      const y = this.year();
      const m = this.month();
      if (!id) return;
      this.attendanceService.getEmployeeMonth(id, y, m).subscribe((records) => {
        const map = new Map<string, AttendanceDto>();
        records.forEach((r) => map.set(r.date, r));
        this.attendanceMap.set(map);
      });
    });
  }

  status(date: string): string {
    return this.attendanceMap().get(date)?.status ?? '';
  }

  statusClass(date: string): string {
    return STATUS_CLASS[this.status(date)] ?? '';
  }

  statusIcon(date: string): string {
    return STATUS_ICON[this.status(date)] ?? '';
  }

  onDayClick(date: string): void {
    const record = this.attendanceMap().get(date);
    if (record && !record.checkOut && record.checkIn) {
      this.attendanceService.checkOut(record.id).subscribe((updated) => {
        this.attendanceMap.update((m) => new Map(m).set(updated.date, updated));
      });
    }
  }

  checkIn(): void {
    this.attendanceService.checkIn(this.employeeId()).subscribe((record) => {
      this.attendanceMap.update((m) => new Map(m).set(record.date, record));
    });
  }

  prevMonth(): void {
    if (this.month() === 1) {
      this.month.set(12);
      this.year.update((y) => y - 1);
    } else {
      this.month.update((m) => m - 1);
    }
  }

  nextMonth(): void {
    if (this.month() === 12) {
      this.month.set(1);
      this.year.update((y) => y + 1);
    } else {
      this.month.update((m) => m + 1);
    }
  }
}
