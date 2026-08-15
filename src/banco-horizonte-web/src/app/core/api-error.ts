import { HttpErrorResponse } from '@angular/common/http';

export function apiErrorMessage(error: unknown, fallback: string): string {
  if (!(error instanceof HttpErrorResponse)) return fallback;
  const detail = error.error?.detail ?? error.error?.message;
  if (typeof detail === 'string' && detail.trim()) return detail;
  if (error.status === 0) return 'No pudimos comunicarnos con el servicio. Revisa tu conexión e intenta nuevamente.';
  if (error.status === 403) return 'No tienes permisos para realizar esta acción.';
  if (error.status === 404) return 'El recurso solicitado ya no está disponible.';
  if (error.status === 429) return 'Se realizaron demasiadas solicitudes. Espera un momento e intenta nuevamente.';
  if (error.status >= 500) return 'El servicio está temporalmente indisponible. Intenta nuevamente en unos minutos.';
  return fallback;
}
