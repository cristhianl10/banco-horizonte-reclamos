import { Routes } from '@angular/router';
import { authGuard, operatorGuard, supervisorGuard } from './core/auth.guard';

export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./pages/login.page').then(m => m.LoginPage) },
  {
    path: '', loadComponent: () => import('./layout/app-shell').then(m => m.AppShell), canActivate: [authGuard], children: [
      { path: 'dashboard', canActivate: [supervisorGuard], loadComponent: () => import('./pages/dashboard.page').then(m => m.DashboardPage) },
      { path: 'reclamos', loadComponent: () => import('./pages/complaints.page').then(m => m.ComplaintsPage) },
      { path: 'reclamos/nuevo', canActivate: [operatorGuard], loadComponent: () => import('./pages/new-complaint.page').then(m => m.NewComplaintPage) },
      { path: 'reclamos/:id', loadComponent: () => import('./pages/complaint-detail.page').then(m => m.ComplaintDetailPage) },
      { path: 'configuracion', canActivate: [supervisorGuard], loadComponent: () => import('./pages/configuration.page').then(m => m.ConfigurationPage) },
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
    ]
  },
  { path: '**', redirectTo: 'dashboard' },
];
