import { Injectable, inject } from '@angular/core';
import { Observable, catchError, tap, throwError } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { InventoryState } from '../../../core/state/inventory.state';
import {
  AddStockMovementDto,
  CreateProductDto,
  ProductDto,
  StockMovementDto,
  UpdateProductDto,
} from '../../../shared/models/inventory.models';

@Injectable({ providedIn: 'root' })
export class InventoryService {
  private readonly api = inject(ApiService);
  private readonly state = inject(InventoryState);

  loadAll(): Observable<ProductDto[]> {
    this.state.setLoading(true);
    return this.api.get<ProductDto[]>('products').pipe(
      tap((data) => {
        this.state.setProducts(data);
        this.state.setLoading(false);
      }),
      catchError((err) => {
        this.state.setLoading(false);
        return throwError(() => err);
      }),
    );
  }

  getLowStock(): Observable<ProductDto[]> {
    return this.api.get<ProductDto[]>('products/low-stock');
  }

  create(dto: CreateProductDto): Observable<ProductDto> {
    return this.api
      .post<ProductDto>('products', dto)
      .pipe(tap((created) => this.state.addProduct(created)));
  }

  update(id: string, dto: UpdateProductDto): Observable<ProductDto> {
    return this.api
      .put<ProductDto>(`products/${id}`, dto)
      .pipe(tap((updated) => this.state.upsertProduct(updated)));
  }

  addMovement(productId: string, dto: AddStockMovementDto): Observable<ProductDto> {
    return this.api
      .post<ProductDto>(`products/${productId}/movements`, dto)
      .pipe(tap((updated) => this.state.upsertProduct(updated)));
  }

  getMovements(productId: string): Observable<StockMovementDto[]> {
    return this.api.get<StockMovementDto[]>(`products/${productId}/movements`);
  }

  delete(id: string): Observable<void> {
    return this.api.delete(`products/${id}`).pipe(tap(() => this.state.removeProduct(id)));
  }
}
