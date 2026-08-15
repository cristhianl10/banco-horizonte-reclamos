import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = async () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  await auth.whenReady();
  return auth.isAuthenticated() ? true : router.createUrlTree(['/login']);
};

export const supervisorGuard: CanActivateFn = async () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  await auth.whenReady();
  return auth.hasAnyRole('Supervisor', 'Administrador') ? true : router.createUrlTree(['/reclamos']);
};

export const operatorGuard: CanActivateFn = async () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  await auth.whenReady();
  return auth.hasAnyRole('Operador', 'Supervisor', 'Administrador') ? true : router.createUrlTree(['/reclamos']);
};
