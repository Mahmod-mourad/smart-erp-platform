import { Routes } from '@angular/router';

export const inventoryRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./products/product-list/product-list').then((m) => m.ProductList),
  },
];
