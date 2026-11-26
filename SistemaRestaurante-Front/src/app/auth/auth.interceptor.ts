import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthService } from '../services/auth.service';

/**
 * Añade el token a las llamadas a la API y, si la API responde 401 (sesión caducada,
 * usuario desactivado o contraseña cambiada), cierra la sesión y lleva al login.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  const isApiCall = req.url.startsWith(environment.apiUrl);
  const isLogin = req.url.endsWith('/Auth/login');
  const token = isApiCall && !isLogin ? auth.token() : null;

  const request = token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;

  return next(request).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && error.status === 401 && isApiCall && !isLogin) {
        auth.logout(router.url);
      }
      return throwError(() => error);
    }),
  );
};
