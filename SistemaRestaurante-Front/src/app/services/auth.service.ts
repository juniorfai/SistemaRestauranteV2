import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  ChangePasswordRequestDTO,
  LoginRequestDTO,
  LoginResponseDTO,
  UserResponseDTO,
  UserRole,
} from '../models/auth.model';

const STORAGE_KEY = 'rm-session';

interface Session {
  token: string;
  expiresAt: string;
  user: UserResponseDTO;
}

/** Roles que pueden dar de alta, editar y borrar mesas, platos y clientes. */
export const MANAGEMENT_ROLES: UserRole[] = ['Admin', 'Manager'];

/** Todo el personal de un restaurante (el administrador de la plataforma no trabaja en ninguno). */
export const RESTAURANT_ROLES: UserRole[] = ['Admin', 'Manager', 'Staff'];

/** Pueden abrir la pantalla de cocina: cocina y, para echar un vistazo, el resto del restaurante. */
export const KITCHEN_SCREEN_ROLES: UserRole[] = [...RESTAURANT_ROLES, 'Kitchen'];

/**
 * Sesión del usuario. El token se guarda en localStorage para que la sesión sobreviva a una
 * recarga de la tableta del restaurante; caduca según lo que diga la API (por defecto 12 h).
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly apiUrl = `${environment.apiUrl}/Auth`;

  private readonly session = signal<Session | null>(readSession());

  readonly user = computed(() => this.session()?.user ?? null);
  readonly isLoggedIn = computed(() => this.session() !== null);
  readonly isAdmin = computed(() => this.user()?.role === 'Admin');
  readonly canManage = computed(() => this.hasRole(MANAGEMENT_ROLES));
  readonly isPlatformAdmin = computed(() => this.user()?.role === 'PlatformAdmin');

  /**
   * Pantalla de inicio según el rol: la plataforma, sus restaurantes; el camarero, la sala;
   * cocina, su pantalla; administrador y encargado, las reservas.
   */
  readonly homeUrl = computed(() => {
    switch (this.user()?.role) {
      case 'PlatformAdmin': return '/restaurantes';
      case 'Staff': return '/sala';
      case 'Kitchen': return '/cocina';
      default: return '/reservas';
    }
  });

  /** Token vigente o null si no hay sesión o ya caducó. */
  token(): string | null {
    const current = this.session();
    if (!current) return null;
    if (isExpired(current)) {
      this.clear();
      return null;
    }
    return current.token;
  }

  hasRole(roles: readonly UserRole[]): boolean {
    const role = this.user()?.role;
    return role !== undefined && roles.includes(role);
  }

  login(credentials: LoginRequestDTO): Observable<LoginResponseDTO> {
    return this.http
      .post<LoginResponseDTO>(`${this.apiUrl}/login`, credentials)
      .pipe(tap((response) => this.store(response)));
  }

  changePassword(dto: ChangePasswordRequestDTO): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/change-password`, dto);
  }

  /** Cierra la sesión y vuelve a la pantalla de acceso. */
  /** Tras renombrar el restaurante, para que el menú lateral lo muestre sin volver a entrar. */
  setRestaurantName(name: string): void {
    const current = this.session();
    if (!current) return;
    this.persist({ ...current, user: { ...current.user, restaurantName: name } });
  }

  logout(returnUrl?: string): void {
    this.clear();
    const queryParams = returnUrl && returnUrl !== '/' ? { returnUrl } : undefined;
    this.router.navigate(['/login'], { queryParams });
  }

  private store(response: LoginResponseDTO): void {
    this.persist({ token: response.token, expiresAt: response.expiresAt, user: response.user });
  }

  private persist(session: Session): void {
    this.session.set(session);
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(session));
    } catch {
      // Sin almacenamiento (modo privado): la sesión dura hasta recargar la página.
    }
  }

  private clear(): void {
    this.session.set(null);
    try {
      localStorage.removeItem(STORAGE_KEY);
    } catch {
      // nada que limpiar
    }
  }
}

function isExpired(session: Session): boolean {
  return new Date(session.expiresAt).getTime() <= Date.now();
}

function readSession(): Session | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    const session = JSON.parse(raw) as Session;
    if (!session?.token || !session.user || isExpired(session)) {
      localStorage.removeItem(STORAGE_KEY);
      return null;
    }
    return session;
  } catch {
    return null;
  }
}
