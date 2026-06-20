// Mirrors NexaFlow.Application.DTOs prediction records (Sprint 7).

export interface MonthlyPrediction {
  month: string; // ISO date (first of month)
  predictedValue: number;
  lowerBound: number;
  upperBound: number;
}

export interface SalesForecastResult {
  isSuccessful: boolean;
  errorMessage?: string | null;
  historicalData: number[];
  predictions: MonthlyPrediction[];
  generatedAt: string;
}

export type RiskLevel = 'High' | 'Medium' | 'Low';

export interface CustomerChurnResult {
  customerId: string;
  customerName: string;
  churnProbability: number; // 0–100
  riskLevel: RiskLevel;
}

export type PredictionConfidence = 'High' | 'Medium' | 'Low';

export interface StockDepletionResult {
  productId: string;
  productName: string;
  currentStock: number;
  minimumStock: number;
  dailyConsumptionRate: number;
  daysUntilDepletion: number | null;
  daysUntilMinimum: number | null;
  predictionConfidence: PredictionConfidence;
}

export interface PredictionDashboardSummary {
  salesForecast: SalesForecastResult;
  stockAlerts: StockDepletionResult[];
  highChurnRisk: CustomerChurnResult[];
  activeCustomers: number;
  openLeads: number;
  employees: number;
  lowStockItems: number;
  generatedAt: string;
}
