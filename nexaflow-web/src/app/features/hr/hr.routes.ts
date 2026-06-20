import { Routes } from '@angular/router';

export const hrRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./employees/employee-list/employee-list').then((m) => m.EmployeeList),
  },
  {
    path: 'leaves',
    loadComponent: () => import('./leaves/leave-list/leave-list').then((m) => m.LeaveList),
  },
  {
    path: 'leaves/apply',
    loadComponent: () =>
      import('./leaves/leave-request-form/leave-request-form').then((m) => m.LeaveRequestForm),
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./employees/employee-detail/employee-detail').then((m) => m.EmployeeDetail),
  },
];
