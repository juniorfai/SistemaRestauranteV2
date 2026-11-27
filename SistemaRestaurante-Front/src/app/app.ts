import { Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { UserRole } from './models/auth.model';
import { AuthService, MANAGEMENT_ROLES, RESTAURANT_ROLES } from './services/auth.service';
import { ThemeService } from './services/theme.service';
import { ROLE_LABELS } from './utils/labels';

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
    { path: '/reservas', label: 'Reservas', icon: 'bi-calendar-check', roles: RESTAURANT_ROLES },
    { path: '/mesas', label: 'Mesas', icon: 'bi-grid-3x3-gap', roles: RESTAURANT_ROLES },
    { path: '/clientes', label: 'Clientes', icon: 'bi-people', roles: RESTAURANT_ROLES },
    { path: '/menu', label: 'Carta', icon: 'bi-journal-richtext', roles: RESTAURANT_ROLES },
    { path: '/zonas', label: 'Zonas', icon: 'bi-fire', roles: MANAGEMENT_ROLES },
    { path: '/personal', label: 'Personal', icon: 'bi-person-badge', roles: ['Admin', 'PlatformAdmin'] },
    { path: '/restaurante', label: 'Mi restaurante', icon: 'bi-shop-window', roles: ['Admin'] },
  ];

  /** Menú según el rol del usuario conectado. */
  readonly navItems = computed(() => {
    this.auth.user();
    return this.allNavItems.filter((item) => !item.roles || this.auth.hasRole(item.roles));
  });

  logout(): void {
    this.auth.logout();
  }
}
