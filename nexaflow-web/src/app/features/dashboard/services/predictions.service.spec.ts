import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { PredictionsService } from './predictions.service';
import { ApiService } from '../../../core/services/api.service';
import { PredictionDashboardSummary } from '../../../shared/models/prediction.models';

function summary(overrides: Partial<PredictionDashboardSummary> = {}): PredictionDashboardSummary {
  return {
    salesForecast: {
      isSuccessful: true,
      historicalData: [1000, 1100],
      predictions: [],
      generatedAt: '2026-06-18T00:00:00Z',
    },
    stockAlerts: [],
    highChurnRisk: [],
    activeCustomers: 12,
    openLeads: 5,
    employees: 3,
    lowStockItems: 2,
    generatedAt: '2026-06-18T00:00:00Z',
    ...overrides,
  };
}

describe('PredictionsService', () => {
  let service: PredictionsService;
  let api: { get: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    api = { get: vi.fn() };
    TestBed.configureTestingModule({
      providers: [PredictionsService, { provide: ApiService, useValue: api }],
    });
    service = TestBed.inject(PredictionsService);
  });

  it('getDashboardSummary hits the summary endpoint', () => {
    const data = summary();
    api.get.mockReturnValue(of(data));

    let received: PredictionDashboardSummary | undefined;
    service.getDashboardSummary().subscribe((r) => (received = r));

    expect(api.get).toHaveBeenCalledWith('predictions/dashboard-summary');
    expect(received).toEqual(data);
  });

  it('getSalesForecast passes monthsAhead as a query param', () => {
    api.get.mockReturnValue(of(summary().salesForecast));

    service.getSalesForecast(6).subscribe();

    expect(api.get).toHaveBeenCalledWith('predictions/sales-forecast?monthsAhead=6');
  });

  it('getChurnRisk and getStockDepletion hit their endpoints', () => {
    api.get.mockReturnValue(of([]));

    service.getChurnRisk().subscribe();
    service.getStockDepletion().subscribe();

    expect(api.get).toHaveBeenCalledWith('predictions/churn-risk');
    expect(api.get).toHaveBeenCalledWith('predictions/stock-depletion');
  });
});
