export type StockMovementType = 'In' | 'Out';

export interface ProductDto {
  id: string;
  name: string;
  sku?: string;
  category?: string;
  unitPrice: number;
  currentStock: number;
  minimumStock: number;
  isLowStock: boolean;
  createdAt: string;
}

export interface CreateProductDto {
  name: string;
  sku?: string;
  category?: string;
  unitPrice: number;
  minimumStock: number;
  description?: string;
}

export interface UpdateProductDto {
  name: string;
  unitPrice: number;
  minimumStock: number;
  description?: string;
}

export interface AddStockMovementDto {
  type: StockMovementType;
  quantity: number;
  reason: string;
}

export interface StockMovementDto {
  id: string;
  type: StockMovementType;
  quantity: number;
  reason: string;
  createdAt: string;
  createdByName?: string;
}
