import { Component, inject } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { InventoryState } from '../../../../core/state/inventory.state';
import { InventoryService } from '../../services/inventory.service';
import { StockMovementDialog } from '../stock-movement-dialog/stock-movement-dialog';
import { ProductFormDialog } from '../product-form-dialog/product-form-dialog';
import {
  AddStockMovementDto,
  CreateProductDto,
  ProductDto,
} from '../../../../shared/models/inventory.models';

/** Product grid with stock bars, low-stock badges and a stock-movement dialog. */
@Component({
  selector: 'app-product-list',
  imports: [
    CurrencyPipe, MatButtonModule, MatIconModule, MatProgressBarModule, MatDialogModule,
  ],
  template: `
    <div class="page-header">
      <h1>Inventory</h1>
      <button mat-flat-button color="primary" (click)="openCreate()">
        <mat-icon>add</mat-icon> New Product
      </button>
    </div>

    @if (inventory.isLoading()) { <mat-progress-bar mode="indeterminate"></mat-progress-bar> }

    @if (!inventory.isLoading() && inventory.products().length === 0) {
      <div class="empty-state">
        <mat-icon>inventory_2</mat-icon>
        <p>No products yet.</p>
      </div>
    } @else {
      <div class="grid">
        @for (p of inventory.products(); track p.id) {
          <div class="card">
            @if (p.isLowStock) { <div class="badge danger">⚠ Low Stock</div> }
            <h3>{{ p.name }}</h3>
            <p class="muted">{{ p.sku || '—' }} · {{ p.category || 'Uncategorized' }}</p>
            <p class="price">{{ p.unitPrice | currency: 'EGP' }}</p>

            <div class="bar"><div class="fill" [style.width.%]="stockPct(p)" [style.background]="barColor(p)"></div></div>
            <p class="stock">{{ p.currentStock }} / {{ p.minimumStock }} minimum</p>

            <button mat-stroked-button (click)="openMovement(p)">
              <mat-icon>swap_vert</mat-icon> Stock Movement
            </button>
          </div>
        }
      </div>
    }
  `,
  styles: `
    .page-header { display: flex; justify-content: space-between; align-items: center; padding: 24px 24px 8px; }
    .grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(260px, 1fr)); gap: 16px; padding: 16px 24px; }
    .card {
      position: relative; padding: 16px; border: 1px solid var(--mat-sys-outline-variant);
      border-radius: 12px; background: var(--mat-sys-surface-container-low);
    }
    .card h3 { margin: 0 0 4px; }
    .muted { color: var(--mat-sys-on-surface-variant); margin: 0 0 8px; font-size: 13px; }
    .price { font-weight: 600; margin: 0 0 12px; }
    .badge { position: absolute; top: 12px; right: 12px; font-size: 11px; padding: 2px 8px; border-radius: 999px; }
    .badge.danger { background: var(--mat-sys-error-container); color: var(--mat-sys-on-error-container); }
    .bar { height: 8px; border-radius: 4px; background: var(--mat-sys-surface-variant); overflow: hidden; }
    .fill { height: 100%; transition: width .2s; }
    .stock { font-size: 13px; color: var(--mat-sys-on-surface-variant); margin: 6px 0 12px; }
    .empty-state { display: grid; place-items: center; gap: 8px; padding: 64px; color: var(--mat-sys-on-surface-variant); }
  `,
})
export class ProductList {
  protected readonly inventory = inject(InventoryState);
  private readonly inventoryService = inject(InventoryService);
  private readonly dialog = inject(MatDialog);

  constructor() {
    this.inventoryService.loadAll().subscribe({ error: () => {} });
  }

  stockPct(p: ProductDto): number {
    if (p.minimumStock <= 0) return 100;
    return Math.min(100, (p.currentStock / p.minimumStock) * 100);
  }

  barColor(p: ProductDto): string {
    const pct = this.stockPct(p);
    if (pct < 50) return 'var(--mat-sys-error)';
    if (pct < 100) return '#f0ad4e';
    return 'var(--mat-sys-tertiary)';
  }

  openCreate(): void {
    this.dialog
      .open(ProductFormDialog)
      .afterClosed()
      .subscribe((result?: CreateProductDto) => {
        if (result) this.inventoryService.create(result).subscribe();
      });
  }

  openMovement(product: ProductDto): void {
    this.dialog
      .open(StockMovementDialog, { data: product })
      .afterClosed()
      .subscribe((result?: AddStockMovementDto) => {
        if (result) this.inventoryService.addMovement(product.id, result).subscribe();
      });
  }
}
