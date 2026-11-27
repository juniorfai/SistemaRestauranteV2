import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { debounceTime, filter, forkJoin } from 'rxjs';
import { BillResponseDTO, OrderResponseDTO, PaymentMethod } from '../../models/order.model';
import { formatMoney } from '../../services/money.service';
import { OrderService } from '../../services/order.service';
import { RealtimeService } from '../../services/realtime.service';
import { apiErrorMessage } from '../../utils/api-error';
import { PAYMENT_METHODS, PAYMENT_METHOD_ICONS, PAYMENT_METHOD_LABELS } from '../../utils/labels';
import {
  TipChoice,
  blockingItems,
  changeDue,
  localDateTime,
  quickCashAmounts,
  tipFor,
} from '../../utils/order-utils';

/**
 * Cobro de una mesa: a un lado la cuenta provisional (para enseñarla o imprimirla) y al otro
 * la propina que decide el cliente y la forma de pago. Al cobrar se cierra la cuenta y se libera
 * la mesa. Con la cuenta ya cerrada sirve para reimprimir el comprobante.
 */
@Component({
  selector: 'app-bill',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './bill.html',
  styleUrl: './bill.css',
})
export class BillComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly orderService = inject(OrderService);
  private readonly realtime = inject(RealtimeService);

  readonly methods = PAYMENT_METHODS;
  readonly methodLabels = PAYMENT_METHOD_LABELS;
  readonly methodIcons = PAYMENT_METHOD_ICONS;

  readonly orderId = signal(0);
  readonly bill = signal<BillResponseDTO | null>(null);
  readonly order = signal<OrderResponseDTO | null>(null);
  readonly loading = signal(true);
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);
  /** Momento en que se sacó la cuenta (para la hora impresa). */
  readonly printedAt = signal(new Date().toISOString());

  readonly tipChoice = signal<TipChoice>('suggested');
  readonly customTip = signal<number | null>(null);
  readonly method = signal<PaymentMethod | null>(null);
  readonly received = signal<number | null>(null);
  readonly confirming = signal(false);

  readonly isOpen = computed(() => this.bill()?.status === 'Open');
  readonly blocking = computed(() => {
    const order = this.order();
    return order ? blockingItems(order) : 0;
  });
  readonly tip = computed(() => {
    const bill = this.bill();
    return bill ? tipFor(this.tipChoice(), bill.suggestedTip, this.customTip()) : 0;
  });
  readonly total = computed(() => (this.bill()?.subtotal ?? 0) + this.tip());
  readonly change = computed(() => changeDue(this.total(), this.received()));
  readonly cashOptions = computed(() => quickCashAmounts(this.total()));
  readonly canCharge = computed(
    () => this.isOpen() && !!this.method() && this.blocking() === 0 && (this.bill()?.lines.length ?? 0) > 0 && !this.busy(),
  );

  constructor() {
    // Si cocina o sala tocan la cuenta mientras se cobra, se actualiza (salvo a mitad de confirmar).
    this.realtime.orderChanged$
      .pipe(
        filter((e) => e.orderId === this.orderId()),
        debounceTime(300),
        takeUntilDestroyed(),
      )
      .subscribe(() => {
        if (!this.busy() && !this.confirming()) this.load(false);
      });
  }

  ngOnInit(): void {
    this.orderId.set(Number(this.route.snapshot.paramMap.get('id')));
    this.load(true);
  }

  load(showSpinner: boolean): void {
    if (showSpinner) this.loading.set(true);
    forkJoin({ bill: this.orderService.getBill(this.orderId()), order: this.orderService.getById(this.orderId()) }).subscribe({
      next: ({ bill, order }) => {
        this.bill.set(bill);
        this.order.set(order);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(apiErrorMessage(err, 'No se pudo cargar la cuenta.'));
      },
    });
  }

  money(amount: number | null | undefined): string {
    return formatMoney(amount, this.bill()?.currencyCode ?? 'COP');
  }

  dateTime(iso: string | null | undefined): string {
    return localDateTime(iso);
  }

  pickTip(choice: TipChoice): void {
    this.tipChoice.set(choice);
    this.confirming.set(false);
  }

  pickMethod(method: PaymentMethod): void {
    this.method.set(method);
    this.confirming.set(false);
    if (method !== 'Cash') this.received.set(null);
  }

  print(): void {
    this.printedAt.set(new Date().toISOString());
    // Deja que Angular pinte la hora antes de abrir el diálogo de impresión.
    setTimeout(() => window.print());
  }

  charge(): void {
    if (!this.canCharge()) return;
    if (!this.confirming()) {
      this.confirming.set(true);
      return;
    }
    const method = this.method();
    if (!method) return;
    this.busy.set(true);
    this.error.set(null);
    this.orderService.close(this.orderId(), { paymentMethod: method, tipAmount: this.tip() }).subscribe({
      next: () => {
        this.busy.set(false);
        this.confirming.set(false);
        this.load(false);
      },
      error: (err) => {
        this.busy.set(false);
        this.confirming.set(false);
        this.error.set(apiErrorMessage(err, 'No se pudo cerrar la cuenta.'));
        this.load(false);
      },
    });
  }

  backToFloor(): void {
    this.router.navigate(['/sala']);
  }
}
