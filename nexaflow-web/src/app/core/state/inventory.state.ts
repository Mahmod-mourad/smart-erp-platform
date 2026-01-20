import { Injectable, computed, signal } from '@angular/core';
import { ProductDto } from '../../shared/models/inventory.models';

/** Shared inventory state — the product grid and low-stock views read the same signals. */
@Injectable({ providedIn: 'root' })
export class InventoryState {
  private readonly _products = signal<ProductDto[]>([]);
  private readonly _isLoading = signal(false);

  readonly products = this._products.asReadonly();
  readonly isLoading = this._isLoading.asReadonly();

  readonly lowStockCount = computed(() => this._products().filter((p) => p.isLowStock).length);

  setProducts(data: ProductDto[]): void {
    this._products.set(data);
  }

  setLoading(value: boolean): void {
    this._isLoading.set(value);
  }

  addProduct(product: ProductDto): void {
    this._products.update((list) => [product, ...list]);
  }

  upsertProduct(updated: ProductDto): void {
    this._products.update((list) => {
      const exists = list.some((p) => p.id === updated.id);
      return exists ? list.map((p) => (p.id === updated.id ? updated : p)) : [updated, ...list];
    });
  }

  removeProduct(id: string): void {
    this._products.update((list) => list.filter((p) => p.id !== id));
  }
}
