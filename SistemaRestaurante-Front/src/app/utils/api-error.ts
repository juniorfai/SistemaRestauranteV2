import { HttpErrorResponse } from '@angular/common/http';

/**
 * Convierte un error de la API (ProblemDetails o errores de validación de ASP.NET) en un texto
 * listo para mostrar al usuario.
 */
export function apiErrorMessage(error: unknown, fallback = 'Ocurrió un error inesperado.'): string {
  if (!(error instanceof HttpErrorResponse)) {
    return fallback;
  }

  if (error.status === 0) {
    return 'No se pudo conectar con el servidor. Verifica que la API esté en ejecución.';
  }

  const body = error.error as
    | { detail?: string; title?: string; errors?: Record<string, string[]> }
    | null
    | undefined;

  if (body?.errors) {
    const messages = Object.values(body.errors).flat();
    if (messages.length > 0) {
      return messages.join(' ');
    }
  }

  return body?.detail || body?.title || fallback;
}
