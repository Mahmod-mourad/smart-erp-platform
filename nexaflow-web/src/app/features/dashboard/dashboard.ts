import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../core/services/auth.service';
import { PredictionsService } from './services/predictions.service';
import { PredictionDashboardSummary } from '../../shared/models/prediction.models';
import { KpiCard, KpiData } from './widgets/kpi-card';
import { SalesForecastChart } from './widgets/sales-forecast-chart';
import { ChurnRiskList } from './widgets/churn-risk-list';
import { StockAlertsWidget } from './widgets/stock-alerts-widget';

@Component({
  selector: 'app-dashboard',
  imports: [
    MatProgressSpinnerModule,
    KpiCard,
    SalesForecastChart,
    ChurnRiskList,
    StockAlertsWidget,
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly predictions = inject(PredictionsService);

  readonly user = this.auth.user;
  readonly isLoading = signal(true);
  readonly error = signal<string | null>(null);
  readonly summary = signal<PredictionDashboardSummary | null>(null);

  readonly kpis = computed<KpiData[]>(() => {
    const s = this.summary();
    if (!s) return [];
    return [
      { label: 'Active Customers', value: s.activeCustomers, icon: 'group', accent: 'info' },
      { label: 'Open Leads', value: s.openLeads, icon: 'trending_up', accent: 'success' },
      { label: 'Employees', value: s.employees, icon: 'badge', accent: 'warning' },
      { label: 'Low Stock Items', value: s.lowStockItems, icon: 'inventory_2', accent: 'danger' },
    ];
  });

  ngOnInit(): void {
    this.predictions.getDashboardSummary().subscribe({
      next: (summary) => {
        this.summary.set(summary);
        this.isLoading.set(false);
      },
      error: () => {
        this.error.set('Failed to load dashboard data.');
        this.isLoading.set(false);
      },
    });
  }
}
