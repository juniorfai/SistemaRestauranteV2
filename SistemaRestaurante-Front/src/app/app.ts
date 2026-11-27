import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { UserRole } from './models/auth.model';
import { AuthService, MANAGEMENT_ROLES, RESTAURANT_ROLES } from './services/auth.service';
import { ItemsReadyEvent, RealtimeService } from './services/realtime.service';
import { ThemeService } from './services/theme.service';
import { ROLE_LABELS } from './utils/labels';

/** Cuánto se queda a la vista el aviso de platos listos. */
const READY_TOAST_MS = 15_000;

interface ReadyToast extends ItemsReadyEvent {
  id: number;
}

interface NavItem {
  path: string;
  label: string;
  icon: string;
  /** Si se indica, solo lo ven estos roles. */
  roles?: UserRole[];
}

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  readonly theme = inject(ThemeService);
  readonly isDark = computed(() => this.theme.theme() === 'dark');

  readonly auth = inject(AuthService);
  readonly roleLabels = ROLE_LABELS;

  private readonly allNavItems: NavItem[] = [
    { path: '/restaurantes', label: 'Restaurantes', icon: 'bi-shop', roles: ['PlatformAdmin'] },
    { path: '/sala', label: 'Sala', icon: 'bi-cup-hot', roles: RESTAURANT_ROLES },
    { path: '/cocina', label: 'Cocina', icon: 'bi-fire', roles: ['Admin', 'Manager', 'Kitchen'] },
    { path: '/caja', label: 'Caja', icon: 'bi-cash-coin', roles: MANAGEMENT_ROLES },
    { path: '/reservas', label: 'Reservas', icon: 'bi-calendar-check', roles: RESTAURANT_ROLES },
    { path: '/mesas', label: 'Mesas', icon: 'bi-grid-3x3-gap', roles: RESTAURANT_ROLES },
    { path: '/clientes', label: 'Clientes', icon: 'bi-people', roles: RESTAURANT_ROLES },
    { path: '/menu', label: 'Carta', icon: 'bi-journal-richtext', roles: RESTAURANT_ROLES },
    { path: '/zonas', label: 'Zonas', icon: 'bi-diagram-3', roles: MANAGEMENT_ROLES },
    { path: '/personal', label: 'Personal', icon: 'bi-person-badge', roles: ['Admin', 'PlatformAdmin'] },
    { path: '/restaurante', label: 'Mi restaurante', icon: 'bi-shop-window', roles: ['Admin'] },
  ];

  /** Menú según el rol del usuario conectado. */
  readonly navItems = computed(() => {
    this.auth.user();
    return this.allNavItems.filter((item) => !item.roles || this.auth.hasRole(item.roles));
  });

  // Tiempo real: el servicio conecta al iniciar sesión y desconecta al salir.
  private readonly realtime = inject(RealtimeService);
  private readonly router = inject(Router);
  private toastSeq = 0;

  /** Avisos de "platos listos" para el camarero de la mesa. */
  readonly readyToasts = signal<ReadyToast[]>([]);

  constructor() {
    const timers = new Set<ReturnType<typeof setTimeout>>();
    this.realtime.itemsReady$
      .pipe(
        filter((e) => e.waiterId === this.auth.user()?.id && this.auth.user()?.role !== 'Kitchen'),
        takeUntilDestroyed(),
      )
      .subscribe((e) => {
        const toast = { ...e, id: ++this.toastSeq };
        this.readyToasts.update((list) => [...list.slice(-3), toast]);
        try {
          navigator.vibrate?.(200);
        } catch {
          // sin vibración
        }
        const timer = setTimeout(() => {
          timers.delete(timer);
          this.dismissToast(toast.id);
        }, READY_TOAST_MS);
        timers.add(timer);
      });
    inject(DestroyRef).onDestroy(() => timers.forEach(clearTimeout));
  }

  openToast(toast: ReadyToast): void {
    this.dismissToast(toast.id);
    this.router.navigate(['/sala/cuenta', toast.orderId]);
  }

  dismissToast(id: number): void {
    this.readyToasts.update((list) => list.filter((t) => t.id !== id));
  }

  logout(): void {
    this.readyToasts.set([]);
    this.auth.logout();
  }
}
