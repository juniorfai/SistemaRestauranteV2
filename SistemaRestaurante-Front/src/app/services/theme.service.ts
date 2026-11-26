import { DOCUMENT } from '@angular/common';
import { Injectable, effect, inject, signal } from '@angular/core';

export type Theme = 'light' | 'dark';

const STORAGE_KEY = 'rm-theme';

/**
 * Modo día / noche. Guarda la preferencia en el navegador; si no hay ninguna,
 * sigue la configuración del sistema operativo.
 */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly document = inject(DOCUMENT);

  readonly theme = signal<Theme>(this.initialTheme());

  constructor() {
    effect(() => {
      const theme = this.theme();
      this.document.documentElement.setAttribute('data-bs-theme', theme);
      try {
        localStorage.setItem(STORAGE_KEY, theme);
      } catch {
        // Sin almacenamiento disponible (modo privado): el tema solo dura la sesión.
      }
    });
  }

  toggle(): void {
    this.theme.update((t) => (t === 'dark' ? 'light' : 'dark'));
  }

  private initialTheme(): Theme {
    try {
      const saved = localStorage.getItem(STORAGE_KEY);
      if (saved === 'light' || saved === 'dark') {
        return saved;
      }
    } catch {
      // Ignorar: se usa la preferencia del sistema.
    }
    const prefersDark = this.document.defaultView?.matchMedia?.('(prefers-color-scheme: dark)').matches;
    return prefersDark ? 'dark' : 'light';
  }
}
