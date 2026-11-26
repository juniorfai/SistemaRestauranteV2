import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { UserRole } from '../models/auth.model';
import { AuthService } from '../services/auth.service';

/** Exige sesión iniciada; si no, manda al login recordando a dónde quería ir. */
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  if (auth.token()) return true;
  return inject(Router).createUrlTree(['/login'], {
    queryParams: state.url && state.url !== '/' ? { returnUrl: state.url } : undefined,
  });
};

/** Exige uno de los roles indicados en la ruta: data: { roles: ['Admin', 'Manager'] }. */
export const roleGuard: CanActivateFn = (route) => {
  const auth = inject(AuthService);
  const roles = (route.data['roles'] as UserRole[] | undefined) ?? [];
  return auth.hasRole(roles) ? true : inject(Router).createUrlTree(['/reservas']);
};

/** La pantalla de login no tiene sentido con sesión iniciada. */
export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  return auth.token() ? inject(Router).createUrlTree(['/reservas']) : true;
};
