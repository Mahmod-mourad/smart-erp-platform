import { Component, input } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { CustomerChurnResult } from '../../../shared/models/prediction.models';

/** Lists customers most likely to churn, with a probability bar and risk badge. */
@Component({
  selector: 'app-churn-risk-list',
  imports: [MatCardModule],
  template: `
    <mat-card>
      <mat-card-header>
        <mat-card-title>⚠ Churn Risk</mat-card-title>
      </mat-card-header>
      <mat-card-content>
        @if (risks()?.length) {
          @for (risk of risks(); track risk.customerId) {
            <div class="risk">
              <span class="risk__name">{{ risk.customerName }}</span>
              <span class="risk__bar">
                <span
                  class="risk__fill"
                  [attr.data-level]="risk.riskLevel"
                  [style.width.%]="risk.churnProbability"
                ></span>
              </span>
              <span class="risk__pct">{{ risk.churnProbability }}%</span>
              <span class="risk__badge" [attr.data-level]="risk.riskLevel">{{
                risk.riskLevel
              }}</span>
            </div>
          }
        } @else {
          <p class="empty">No churn risk to report.</p>
        }
      </mat-card-content>
    </mat-card>
  `,
  styles: [
    `
      .risk {
        display: grid;
        grid-template-columns: 1fr 120px 44px 64px;
        align-items: center;
        gap: 10px;
        padding: 8px 0;
      }
      .risk__name {
        font-weight: 500;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
      }
      .risk__bar {
        height: 8px;
        border-radius: 4px;
        background: rgba(0, 0, 0, 0.08);
        overflow: hidden;
      }
      .risk__fill {
        display: block;
        height: 100%;
      }
      .risk__fill[data-level='High'] {
        background: #ef4444;
      }
      .risk__fill[data-level='Medium'] {
        background: #f59e0b;
      }
      .risk__fill[data-level='Low'] {
        background: #10b981;
      }
      .risk__pct {
        font-variant-numeric: tabular-nums;
        text-align: right;
      }
      .risk__badge {
        font-size: 11px;
        font-weight: 600;
        text-align: center;
        padding: 2px 8px;
        border-radius: 999px;
        color: #fff;
      }
      .risk__badge[data-level='High'] {
        background: #ef4444;
      }
      .risk__badge[data-level='Medium'] {
        background: #f59e0b;
      }
      .risk__badge[data-level='Low'] {
        background: #10b981;
      }
      .empty {
        color: rgba(0, 0, 0, 0.55);
        padding: 16px 4px;
      }
    `,
  ],
})
export class ChurnRiskList {
  readonly risks = input<CustomerChurnResult[] | null>();
}
