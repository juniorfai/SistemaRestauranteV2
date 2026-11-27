import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { OrderResponseDTO } from '../../models/order.model';
import { MoneyPipe } from '../../services/money.service';
import { OrderService } from '../../services/order.service';
import { apiErrorMessage } from '../../utils/api-error';
import { PAYMENT_METHOD_ICONS, PAYMENT_METHOD_LABELS, toDateInputValue } from '../../utils/labels';
import { localTime, summarizeDay } from '../../utils/order-utils';

/** Caja del día: lo cobrado por forma de pago, las propinas y las cuentas cerradas o anuladas. */
@Component({
  selector: 'app-cash-report',
  standalone: true,
  imports: [FormsModule, MoneyPipe],
  templateUrl: './cash-report.html',
  styleUrl: './cash-report.css',
})
export class CashReportComponent implements OnInit {
  private readonly orderService = inject(OrderService);
  private readonly router = inject(Router);

  readonly methodLabels = PAYMENT_METHOD_LABELS;
  readonly methodIcons = PAYMENT_METHOD_ICONS;
  readonly today = toDateInputValue(new Date());

  readonly date = signal(this.today);
  readonly orders = signal<OrderResponseDTO[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  readonly summary = computed(() => summarizeDay(this.orders()));
  /** Las más recientes primero. */
  readonly rows = computed(() => [...this.orders()].sort((a, b) => (b.closedAt ?? '').localeCompare(a.closedAt ?? '')));

  ngOnInit(): void {
    this.load();
  }

  changeDate(value: string): void {
    if (!value) return;
    this.date.set(value);
    this.load();
  }

  shiftDay(days: number): void {
    const [y, m, d] = this.date().split('-').map(Number);
    const next = toDateInputValue(new Date(y, m - 1, d + days));
    if (next > this.today) return;
    this.changeDate(next);
  }

  load(): void {
    this.loading.set(true);
    this.orderService.getHistory(this.date()).subscribe({
      next: (orders) => {
        this.orders.set(orders);
        this.loading.set(false);
        this.error.set(null);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(apiErrorMessage(err, 'No se pudo cargar la caja del día.'));
      },
    });
  }

  open(order: OrderResponseDTO): void {
    if (order.status === 'Closed') this.router.navigate(['/sala/cuenta', order.id, 'cobrar']);
    else this.router.navigate(['/sala/cuenta', order.id]);
  }

  time(iso: string | null | undefined): string {
    return localTime(iso);
  }
}
