import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, from, switchMap, throwError } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  if (!request.url.startsWith(environment.apiUrl)) return next(request);
  const auth = inject(AuthService);
  const send = () => {
    const token = auth.accessToken;
    const authenticated = token ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : request;
    return next(authenticated).pipe(catchError(error => {
      if (error instanceof HttpErrorResponse && error.status === 401) auth.logout();
      return throwError(() => error);
    }));
  };
  return from(auth.refreshSession()).pipe(
    catchError(error => { auth.logout(); return throwError(() => error); }),
    switchMap(send)
  );
};
