import { Component, input } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { StockDepletionResult } from '../../../shared/models/prediction.models';

/** Highlights products about to fall below their minimum stock. */
@Component({
  selector: 'app-stock-alerts-widget',
  imports: [MatCardModule],
  template: `
    <mat-card>
      <mat-card-header>
        <mat-card-title>📦 Stock Alerts</mat-card-title>
      </mat-card-header>
      <mat-card-content>
        @if (alerts()?.length) {
          @for (alert of alerts(); track alert.productId) {
            <div class="alert">
              <span class="alert__name">{{ alert.productName }}</span>
              <span class="alert__stock"
                >{{ alert.currentStock }} / min {{ alert.minimumStock }}</span
              >
              <span class="alert__days">
                @if (alert.daysUntilMinimum !== null) {
                  ~{{ alert.daysUntilMinimum }}d to min
                } @else {
                  no recent usage
                }
              </span>
            </div>
          }
        } @else {
          <p class="empty">No stock running low. 🎉</p>
        }
      </mat-card-content>
    </mat-card>
  `,
  styles: [
    `
      .alert {
        display: grid;
        grid-template-columns: 1fr auto auto;
        align-items: center;
        gap: 12px;
        padding: 8px 0;
        border-bottom: 1px solid rgba(0, 0, 0, 0.06);
      }
      .alert:last-child {
        border-bottom: none;
      }
      .alert__name {
        font-weight: 500;
      }
      .alert__stock {
        font-variant-numeric: tabular-nums;
        color: rgba(0, 0, 0, 0.6);
      }
      .alert__days {
        font-size: 13px;
        font-weight: 600;
        color: #ef4444;
      }
      .empty {
        color: rgba(0, 0, 0, 0.55);
        padding: 16px 4px;
      }
    `,
  ],
})
export class StockAlertsWidget {
  readonly alerts = input<StockDepletionResult[] | null>();
}
