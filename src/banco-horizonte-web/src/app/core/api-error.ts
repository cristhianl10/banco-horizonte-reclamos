import { HttpErrorResponse } from '@angular/common/http';

export function apiErrorMessage(error: unknown, fallback: string): string {
  if (!(error instanceof HttpErrorResponse)) return fallback;
  const payload = error.error ?? {};
  const detail = payload.detail ?? payload.message ?? payload.title;
  if (typeof detail === 'string' && detail.trim()) return detail;
  if (payload.errors && typeof payload.errors === 'object') {
    const messages = Object.values(payload.errors as Record<string, unknown>)
      .flatMap(value => Array.isArray(value) ? value : [value])
      .filter((value): value is string => typeof value === 'string' && value.trim().length > 0);
    if (messages.length) return messages.join(' ');
  }
  if (error.status === 0) return 'No pudimos comunicarnos con el servicio. Revisa tu conexión e intenta nuevamente.';
  if (error.status === 401) return 'Tu sesión expiró. Vuelve a iniciar sesión.';
  if (error.status === 403) return 'No tienes permisos para realizar esta acción. Verifica que tu cuenta tenga el rol Operador.';
  if (error.status === 404) return 'El recurso solicitado ya no está disponible.';
  if (error.status === 409) return 'La operación entra en conflicto con un registro existente. Revisa los datos e intenta nuevamente.';
  if (error.status === 429) return 'Se realizaron demasiadas solicitudes. Espera un momento e intenta nuevamente.';
  if (error.status >= 500) return 'El servicio está temporalmente indisponible. Intenta nuevamente en unos minutos.';
  return fallback;
}
