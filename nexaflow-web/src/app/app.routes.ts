import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  // Public auth pages
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/login/login').then((m) => m.Login),
  },
  {
    path: 'register',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/register/register').then((m) => m.Register),
  },
  {
    path: 'accept-invite',
    loadComponent: () =>
      import('./features/auth/accept-invite/accept-invite').then((m) => m.AcceptInvite),
  },

  // Authenticated app (inside the layout shell)
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./layout/shell/shell').then((m) => m.Shell),
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      {
        path: 'dashboard',
        loadComponent: () => import('./features/dashboard/dashboard').then((m) => m.Dashboard),
      },
      {
        path: 'team',
        loadComponent: () => import('./features/team/team').then((m) => m.Team),
      },
      {
        path: 'crm',
        loadChildren: () => import('./features/crm/crm.routes').then((m) => m.crmRoutes),
      },
      {
        path: 'hr',
        loadChildren: () => import('./features/hr/hr.routes').then((m) => m.hrRoutes),
      },
      {
        path: 'inventory',
        loadChildren: () =>
          import('./features/inventory/inventory.routes').then((m) => m.inventoryRoutes),
      },
      {
        path: 'automation',
        loadChildren: () =>
          import('./features/automation/automation.routes').then((m) => m.automationRoutes),
      },
      {
        path: 'integrations',
        loadChildren: () =>
          import('./features/integrations/integrations.routes').then((m) => m.integrationsRoutes),
      },
      {
        path: 'branches',
        loadComponent: () => import('./features/settings/branches/branch-list').then(m => m.BranchList)
      },
      {
        path: 'roles',
        loadComponent: () => import('./features/settings/roles/role-list').then(m => m.RoleList)
      },
      {
        path: 'chart-of-accounts',
        loadComponent: () => import('./features/accounting/chart-of-accounts/chart-of-accounts').then(m => m.ChartOfAccounts)
      },
      {
        path: 'journals',
        loadComponent: () => import('./features/accounting/journal-entries/journal-list').then(m => m.JournalList)
      },
      {
        path: 'backup',
        loadComponent: () => import('./features/settings/backup/backup').then(m => m.BackupComponent)
      },
    ],
  },

  { path: '**', redirectTo: '' },
];
