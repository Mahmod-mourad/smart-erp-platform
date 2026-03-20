import { Routes } from '@angular/router';

export const crmRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./customers/customer-list/customer-list').then((m) => m.CustomerList),
  },
  {
    path: 'pipeline',
    loadComponent: () =>
      import('./pipeline/kanban-board/kanban-board').then((m) => m.KanbanBoard),
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./customers/customer-detail/customer-detail').then((m) => m.CustomerDetail),
  },
];
