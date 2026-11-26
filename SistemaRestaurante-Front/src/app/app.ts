import { Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ThemeService } from './services/theme.service';

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

  readonly navItems = [
    { path: '/reservas', label: 'Reservas', icon: 'bi-calendar-check' },
    { path: '/mesas', label: 'Mesas', icon: 'bi-grid-3x3-gap' },
    { path: '/clientes', label: 'Clientes', icon: 'bi-people' },
    { path: '/menu', label: 'Carta', icon: 'bi-journal-richtext' },
  ];
}
