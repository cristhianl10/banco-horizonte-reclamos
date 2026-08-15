import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = async () => {
  const auth = inject(AuthService);
  await auth.whenReady();
  return auth.isAuthenticated() ? true : inject(Router).createUrlTree(['/login']);
};

export const supervisorGuard: CanActivateFn = async () => {
  const auth = inject(AuthService);
  await auth.whenReady();
  return auth.hasAnyRole('Supervisor', 'Administrador') ? true : inject(Router).createUrlTree(['/reclamos']);
};

export const operatorGuard: CanActivateFn = async () => {
  const auth = inject(AuthService);
  await auth.whenReady();
  return auth.hasAnyRole('Operador', 'Supervisor', 'Administrador') ? true : inject(Router).createUrlTree(['/reclamos']);
};
