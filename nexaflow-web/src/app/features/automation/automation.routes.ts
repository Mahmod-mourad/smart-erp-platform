import { Routes } from '@angular/router';

export const automationRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./workflow-list/workflow-list').then((m) => m.WorkflowList),
  },
  {
    path: 'new',
    loadComponent: () =>
      import('./workflow-builder/workflow-builder').then((m) => m.WorkflowBuilder),
  },
  {
    path: ':id/logs',
    loadComponent: () =>
      import('./workflow-logs/workflow-logs').then((m) => m.WorkflowLogs),
  },
];
