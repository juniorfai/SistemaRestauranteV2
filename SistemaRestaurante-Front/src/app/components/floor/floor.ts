import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { debounceTime, forkJoin, merge } from 'rxjs';
import { OrderResponseDTO } from '../../models/order.model';
import { ReservationResponseDTO } from '../../models/reservation.model';
import { TableResponseDTO } from '../../models/table.model';
import { AuthService } from '../../services/auth.service';
import { MoneyPipe } from '../../services/money.service';
import { OrderService } from '../../services/order.service';
import { RealtimeService } from '../../services/realtime.service';
import { ReservationService } from '../../services/reservation.service';
import { TableService } from '../../services/table.service';
import { apiErrorMessage } from '../../utils/api-error';
import { toDateInputValue } from '../../utils/labels';
import { OrderSummary, formatElapsed, minutesSince, summarize } from '../../utils/order-utils';

/** Cada cuánto se refresca el plano si se cae el tiempo real (y se actualizan los minutos). */
const REFRESH_MS = 15_000;

interface TableTile {
  table: TableResponseDTO;
  order: OrderResponseDTO | null;
  summary: OrderSummary | null;
  /** Próxima reserva de hoy para esta mesa (pendiente o confirmada). */
  reservation: ReservationResponseDTO | null;
}

/** Plano de sala: qué mesas están libres, ocupadas o esperando a una reserva. */
@Component({
  selector: 'app-floor',
  standalone: true,
  imports: [FormsModule, MoneyPipe],
  templateUrl: './floor.html',
  styleUrl: './floor.css',
})
export class FloorComponent implements OnInit {
  private readonly tableService = inject(TableService);
  private readonly orderService = inject(OrderService);
  private readonly reservationService = inject(ReservationService);
  private readonly router = inject(Router);

  readonly tables = signal<TableResponseDTO[]>([]);
  readonly orders = signal<OrderResponseDTO[]>([]);
  readonly reservations = signal<ReservationResponseDTO[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly now = signal(Date.now());

  readonly zone = signal<string | null>(null);
  readonly onlyMine = signal(false);

  /** Mesa que se está abriendo (formulario de sentar clientes). */
  readonly seating = signal<TableResponseDTO | null>(null);
  readonly guests = signal(2);
  readonly reservationId = signal<number | null>(null);
  readonly notes = signal('');
  readonly opening = signal(false);

  readonly zones = computed(() =>
    [...new Set(this.tables().map((t) => t.location?.trim() || 'Sala'))].sort((a, b) => a.localeCompare(b)),
  );

  readonly tiles = computed<TableTile[]>(() => {
    const byTable = new Map(this.orders().map((o) => [o.tableId, o]));
    const upcoming = this.reservations()
      .filter((r) => r.status === 'Pending' || r.status === 'Confirmed')
      .sort((a, b) => a.reservationDateTime.localeCompare(b.reservationDateTime));
    const zone = this.zone();
    return [...this.tables()]
      .sort((a, b) => a.tableNumber - b.tableNumber)
      .filter((t) => !zone || (t.location?.trim() || 'Sala') === zone)
      .map((table) => {
        const order = byTable.get(table.id) ?? null;
        return {
          table,
          order,
          summary: order ? summarize(order) : null,
          reservation: upcoming.find((r) => r.tableId === table.id) ?? null,
        };
      })
      .filter((t) => !this.onlyMine() || t.order?.waiterId === this.myId());
  });

  readonly stats = computed(() => {
    const open = this.orders();
    return {
      occupied: open.length,
      free: this.tables().filter((t) => t.status !== 'OutOfService').length - open.length,
      ready: open.reduce((n, o) => n + summarize(o).ready, 0),
      guests: open.reduce((n, o) => n + o.guests, 0),
    };
  });

  /** Reservas de hoy que aún pueden sentarse, la de esta mesa primero. */
  readonly seatableReservations = computed(() => {
    const table = this.seating();
    return this.reservations()
      .filter((r) => r.status === 'Pending' || r.status === 'Confirmed')
      .sort((a, b) => Number(b.tableId === table?.id) - Number(a.tableId === table?.id) ||
        a.reservationDateTime.localeCompare(b.reservationDateTime));
  });

  private readonly auth = inject(AuthService);
  private readonly myId = computed(() => this.auth.user()?.id ?? null);

  private readonly realtime = inject(RealtimeService);

  constructor() {
    // Cualquier cambio en una cuenta (o al reconectar) actualiza el plano; si varios llegan
    // seguidos, se recarga una sola vez.
    merge(this.realtime.orderChanged$, this.realtime.resync$)
      .pipe(debounceTime(500), takeUntilDestroyed())
      .subscribe(() => {
        if (!this.seating()) this.load(false);
      });

    const timer = setInterval(() => {
      this.now.set(Date.now());
      if (this.realtime.status() !== 'online' && !document.hidden && !this.seating()) this.load(false);
    }, REFRESH_MS);
    inject(DestroyRef).onDestroy(() => clearInterval(timer));
  }

  ngOnInit(): void {
    this.load(true);
  }

  load(showSpinner: boolean): void {
    if (showSpinner) this.loading.set(true);
    forkJoin({
      tables: this.tableService.getTables(),
      orders: this.orderService.getOpen(),
      reservations: this.reservationService.getByDate(toDateInputValue(new Date())),
    }).subscribe({
      next: ({ tables, orders, reservations }) => {
        this.tables.set(tables);
        this.orders.set(orders);
        this.reservations.set(reservations);
        this.loading.set(false);
        this.error.set(null);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(apiErrorMessage(err, 'No se pudo cargar la sala.'));
      },
    });
  }

  select(tile: TableTile): void {
    if (tile.order) {
      this.router.navigate(['/sala/cuenta', tile.order.id]);
      return;
    }
    if (tile.table.status === 'OutOfService') return;
    this.seating.set(tile.table);
    this.guests.set(tile.reservation?.numberOfPeople ?? Math.min(2, tile.table.capacity));
    this.reservationId.set(null);
    this.notes.set('');
  }

  pickReservation(id: number | null): void {
    this.reservationId.set(id);
    const reservation = this.reservations().find((r) => r.id === id);
    if (reservation) this.guests.set(reservation.numberOfPeople);
  }

  changeGuests(delta: number): void {
    this.guests.update((g) => Math.min(50, Math.max(1, g + delta)));
  }

  open(): void {
    const table = this.seating();
    if (!table) return;
    this.opening.set(true);
    this.orderService
      .open({ tableId: table.id, guests: this.guests(), reservationId: this.reservationId(), notes: this.notes().trim() || null })
      .subscribe({
        next: (order) => {
          this.opening.set(false);
          this.seating.set(null);
          this.router.navigate(['/sala/cuenta', order.id]);
        },
        error: (err) => {
          this.opening.set(false);
          this.error.set(apiErrorMessage(err, 'No se pudo abrir la mesa.'));
          this.seating.set(null);
          this.load(false);
        },
      });
  }

  elapsed(order: OrderResponseDTO): string {
    return formatElapsed(minutesSince(order.openedAt, this.now()));
  }

  reservationTime(r: ReservationResponseDTO): string {
    return r.reservationDateTime.substring(11, 16);
  }
}
