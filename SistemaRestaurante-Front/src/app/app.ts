import { Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { UserRole } from './models/auth.model';
import { AuthService } from './services/auth.service';
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
    { path: '/reservas', label: 'Reservas', icon: 'bi-calendar-check' },
    { path: '/mesas', label: 'Mesas', icon: 'bi-grid-3x3-gap' },
    { path: '/clientes', label: 'Clientes', icon: 'bi-people' },
    { path: '/menu', label: 'Carta', icon: 'bi-journal-richtext' },
    { path: '/personal', label: 'Personal', icon: 'bi-person-badge', roles: ['Admin'] },
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
