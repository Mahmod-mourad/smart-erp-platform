import { Component, input } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';

export type KpiAccent = 'info' | 'success' | 'warning' | 'danger';

export interface KpiData {
  label: string;
  value: string | number;
  icon: string; // Material icon name
  accent: KpiAccent;
}

@Component({
  selector: 'app-kpi-card',
  imports: [MatCardModule, MatIconModule],
  template: `
    <mat-card class="kpi" [attr.data-accent]="kpi().accent">
      <mat-card-content class="kpi__content">
        <span class="kpi__icon"
          ><mat-icon>{{ kpi().icon }}</mat-icon></span
        >
        <span class="kpi__text">
          <span class="kpi__value">{{ kpi().value }}</span>
          <span class="kpi__label">{{ kpi().label }}</span>
        </span>
      </mat-card-content>
    </mat-card>
  `,
  styles: [
    `
      .kpi__content {
        display: flex;
        align-items: center;
        gap: 14px;
      }
      .kpi__icon {
        display: grid;
        place-items: center;
        width: 48px;
        height: 48px;
        border-radius: 12px;
        color: #fff;
      }
      .kpi[data-accent='info'] .kpi__icon {
        background: #2563eb;
      }
      .kpi[data-accent='success'] .kpi__icon {
        background: #10b981;
      }
      .kpi[data-accent='warning'] .kpi__icon {
        background: #f59e0b;
      }
      .kpi[data-accent='danger'] .kpi__icon {
        background: #ef4444;
      }
      .kpi__text {
        display: flex;
        flex-direction: column;
      }
      .kpi__value {
        font-size: 24px;
        font-weight: 700;
        line-height: 1.1;
      }
      .kpi__label {
        font-size: 13px;
        color: rgba(0, 0, 0, 0.55);
      }
    `,
  ],
})
export class KpiCard {
  readonly kpi = input.required<KpiData>();
}
