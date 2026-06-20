import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import {
  CustomerChurnResult,
  PredictionDashboardSummary,
  SalesForecastResult,
  StockDepletionResult,
} from '../../../shared/models/prediction.models';

/** Reads ML.NET predictions for the current tenant from the API. */
@Injectable({ providedIn: 'root' })
export class PredictionsService {
  private readonly api = inject(ApiService);

  getDashboardSummary(): Observable<PredictionDashboardSummary> {
    return this.api.get<PredictionDashboardSummary>('predictions/dashboard-summary');
  }

  getSalesForecast(monthsAhead = 3): Observable<SalesForecastResult> {
    return this.api.get<SalesForecastResult>(
      `predictions/sales-forecast?monthsAhead=${monthsAhead}`,
    );
  }

  getChurnRisk(): Observable<CustomerChurnResult[]> {
    return this.api.get<CustomerChurnResult[]>('predictions/churn-risk');
  }

  getStockDepletion(): Observable<StockDepletionResult[]> {
    return this.api.get<StockDepletionResult[]>('predictions/stock-depletion');
  }
}
