import { Injectable, Pipe, PipeTransform, inject, signal } from '@angular/core';
import { RestaurantService } from './restaurant.service';
import { AuthService } from './auth.service';

/** Monedas sin decimales en la práctica (el peso colombiano no usa centavos). */
const WHOLE_UNIT_CURRENCIES = new Set(['COP', 'CLP', 'PYG', 'JPY']);

const formatters = new Map<string, Intl.NumberFormat>();

/** "$ 25.000" para COP; "US$ 12,50" para USD... Siempre con formato colombiano. */
export function formatMoney(amount: number | null | undefined, currencyCode = 'COP'): string {
  if (amount === null || amount === undefined || Number.isNaN(amount)) return '—';
  let formatter = formatters.get(currencyCode);
  if (!formatter) {
    const digits = WHOLE_UNIT_CURRENCIES.has(currencyCode) ? 0 : 2;
    formatter = new Intl.NumberFormat('es-CO', {
      style: 'currency',
      currency: currencyCode,
      minimumFractionDigits: digits,
      maximumFractionDigits: digits,
    });
    formatters.set(currencyCode, formatter);
  }
  return formatter.format(amount);
}

/**
 * Moneda y propina sugerida del restaurante del usuario. Se piden una vez por sesión;
 * mientras llegan, se asume COP (el mercado inicial).
 */
@Injectable({ providedIn: 'root' })
export class MoneyService {
  private readonly restaurants = inject(RestaurantService);
  private readonly auth = inject(AuthService);

  readonly currencyCode = signal('COP');
  readonly suggestedTipPercent = signal(10);
  private loadedFor: number | null = null;

  /** Carga los datos del restaurante si el usuario cambió (o aún no se habían pedido). */
  ensureLoaded(): void {
    const restaurantId = this.auth.user()?.restaurantId ?? null;
    if (restaurantId === null || restaurantId === this.loadedFor) return;
    this.loadedFor = restaurantId;
    this.restaurants.getCurrent().subscribe({
      next: (r) => {
        this.currencyCode.set(r.currencyCode || 'COP');
        this.suggestedTipPercent.set(r.suggestedTipPercent ?? 10);
      },
      error: () => (this.loadedFor = null),
    });
  }

  format(amount: number | null | undefined): string {
    this.ensureLoaded();
    return formatMoney(amount, this.currencyCode());
  }
}

/** {{ precio | money }}: importe en la moneda del restaurante. */
@Pipe({ name: 'money', standalone: true, pure: false })
export class MoneyPipe implements PipeTransform {
  private readonly money = inject(MoneyService);

  transform(amount: number | null | undefined): string {
    return this.money.format(amount);
  }
}
