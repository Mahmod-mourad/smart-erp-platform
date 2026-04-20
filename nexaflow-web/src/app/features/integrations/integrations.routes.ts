import { Routes } from '@angular/router';

export const integrationsRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./integration-list/integration-list').then((m) => m.IntegrationList),
  },
];
