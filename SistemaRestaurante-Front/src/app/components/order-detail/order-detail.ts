import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Observable, debounceTime, filter, forkJoin, merge, of, switchMap } from 'rxjs';
import { MenuItemCategory, MenuItemResponseDTO } from '../../models/menu.model';
import { OrderCourse, OrderItemResponseDTO, OrderResponseDTO } from '../../models/order.model';
import { TableResponseDTO } from '../../models/table.model';
import { AuthService } from '../../services/auth.service';
import { MenuService } from '../../services/menu.service';
import { MoneyPipe } from '../../services/money.service';
import { OrderService } from '../../services/order.service';
import { RealtimeService } from '../../services/realtime.service';
import { TableService } from '../../services/table.service';
import { apiErrorMessage } from '../../utils/api-error';
import {
  ALLERGEN_LABELS,
  COURSES,
  COURSE_LABELS,
  ITEM_STATUS_BADGES,
  ITEM_STATUS_LABELS,
  MENU_CATEGORIES,
  MENU_CATEGORY_LABELS,
} from '../../utils/labels';
import {
  BatchLine,
  addToBatch,
  batchTotal,
  formatElapsed,
  groupByCourse,
  heldCourses,
  minutesSince,
  summarize,
  toAddItems,
} from '../../utils/order-utils';

/** Sin tiempo real (conexión caída) se recarga cada 10 s. */
const REFRESH_MS = 10_000;
const BATCH_KEY = (orderId: number) => `rm-batch-${orderId}`;

/**
 * Cuenta de una mesa: la carta a un lado y la comanda al otro. Lo que se toca se apunta en la
 * tableta (sobrevive a una recarga) y se manda a la API al enviar a cocina, con un identificador
 * por plato para que un reintento no lo duplique.
 */
@Component({
  selector: 'app-order-detail',
  standalone: true,
  imports: [FormsModule, RouterLink, MoneyPipe],
  templateUrl: './order-detail.html',
  styleUrl: './order-detail.css',
})
export class OrderDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly orderService = inject(OrderService);
  private readonly menuService = inject(MenuService);
  private readonly tableService = inject(TableService);
  readonly auth = inject(AuthService);

  readonly categories = MENU_CATEGORIES;
  readonly categoryLabels = MENU_CATEGORY_LABELS;
  readonly courses = COURSES;
  readonly courseLabels = COURSE_LABELS;
  readonly statusLabels = ITEM_STATUS_LABELS;
  readonly statusBadges = ITEM_STATUS_BADGES;
  readonly allergenLabels = ALLERGEN_LABELS;

  readonly orderId = signal(0);
  readonly order = signal<OrderResponseDTO | null>(null);
  readonly menu = signal<MenuItemResponseDTO[]>([]);
  readonly batch = signal<BatchLine[]>([]);
  readonly loading = signal(true);
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);
  readonly notice = signal<string | null>(null);
  readonly now = signal(Date.now());

  readonly category = signal<MenuItemCategory | null>(null);
  readonly search = signal('');
  readonly holdMains = signal(false);
  /** Línea apuntada cuyas notas se están editando. */
  readonly editingKey = signal<string | null>(null);
  readonly moving = signal(false);
  readonly freeTables = signal<TableResponseDTO[]>([]);

  readonly isOpen = computed(() => this.order()?.status === 'Open');

  readonly visibleMenu = computed(() => {
    const category = this.category();
    const term = normalize(this.search());
    return this.menu().filter(
      (m) => (!category || m.category === category) && (!term || normalize(m.name).includes(term)),
    );
  });

  readonly drafts = computed(() => this.order()?.items.filter((i) => i.status === 'Draft') ?? []);
  readonly sentGroups = computed(() =>
    groupByCourse(this.order()?.items.filter((i) => i.status !== 'Draft') ?? []),
  );
  readonly summary = computed(() => (this.order() ? summarize(this.order()!) : null));
  readonly held = computed(() => (this.order() ? heldCourses(this.order()!) : []));
  readonly pendingCount = computed(
    () => this.batch().reduce((n, l) => n + l.quantity, 0) + this.drafts().reduce((n, i) => n + i.quantity, 0),
  );
  readonly pendingTotal = computed(
    () => batchTotal(this.batch()) + this.drafts().reduce((sum, i) => sum + i.lineTotal, 0),
  );
  /** Retener solo tiene sentido si hay algo de plato fuerte o postre en lo que se va a enviar. */
  readonly canHold = computed(
    () =>
      this.batch().some((l) => l.course === 'Main' || l.course === 'Dessert') ||
      this.drafts().some((i) => i.course === 'Main' || i.course === 'Dessert'),
  );

  private readonly realtime = inject(RealtimeService);

  constructor() {
    // Cocina u otro camarero tocaron esta cuenta: se recarga al momento.
    merge(this.realtime.orderChanged$.pipe(filter((e) => e.orderId === this.orderId())), this.realtime.resync$)
      .pipe(debounceTime(300), takeUntilDestroyed())
      .subscribe(() => {
        if (!this.busy() && this.order()) this.refresh();
      });

    const timer = setInterval(() => {
      this.now.set(Date.now());
      if (this.realtime.status() !== 'online' && !document.hidden && !this.busy() && this.isOpen()) this.refresh();
    }, REFRESH_MS);
    inject(DestroyRef).onDestroy(() => clearInterval(timer));
  }

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.orderId.set(id);
    this.batch.set(readBatch(id));
    forkJoin({ order: this.orderService.getById(id), menu: this.menuService.getMenuItems(false) }).subscribe({
      next: ({ order, menu }) => {
        this.order.set(order);
        this.menu.set(menu);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(apiErrorMessage(err, 'No se pudo cargar la cuenta.'));
      },
    });
  }

  refresh(): void {
    this.orderService.getById(this.orderId()).subscribe({
      next: (order) => this.order.set(order),
      error: () => {
        // Sin conexión un momento: se reintenta en el siguiente ciclo.
      },
    });
  }

  // ---------- Apuntar ----------

  add(dish: MenuItemResponseDTO): void {
    if (!this.isOpen()) return;
    this.setBatch(addToBatch(this.batch(), dish));
  }

  changeQuantity(line: BatchLine, delta: number): void {
    const quantity = line.quantity + delta;
    this.setBatch(
      quantity <= 0
        ? this.batch().filter((l) => l.key !== line.key)
        : this.batch().map((l) => (l.key === line.key ? { ...l, quantity: Math.min(99, quantity) } : l)),
    );
  }

  updateLine(line: BatchLine, changes: Partial<Pick<BatchLine, 'notes' | 'course'>>): void {
    this.setBatch(this.batch().map((l) => (l.key === line.key ? { ...l, ...changes } : l)));
  }

  clearBatch(): void {
    if (this.batch().length && confirm('¿Borrar lo apuntado sin enviar?')) this.setBatch([]);
  }

  /** Borrador que ya estaba en la cuenta (p. ej. el pedido anticipado de una reserva). */
  changeDraft(item: OrderItemResponseDTO, delta: number): void {
    const quantity = item.quantity + delta;
    const request$ =
      quantity <= 0
        ? this.orderService.removeDraft(this.orderId(), item.id)
        : this.orderService.updateDraft(this.orderId(), item.id, {
            quantity: Math.min(99, quantity),
            notes: item.notes,
            course: item.course,
          });
    this.run(request$, null);
  }

  // ---------- Enviar ----------

  /** Manda a la API lo apuntado en la tableta y, si se pide, lo envía a cocina. */
  send(toKitchen: boolean): void {
    const order = this.order();
    if (!order || this.busy()) return;
    const lines = this.batch();
    const hold: OrderCourse | null = toKitchen && this.holdMains() && this.canHold() ? 'Main' : null;

    const save$: Observable<unknown> = lines.length
      ? this.orderService.addItems(order.id, toAddItems(lines))
      : of(null);
    const request$ = save$.pipe(
      switchMap(() => {
        // Ya está en la API: se puede olvidar en la tableta aunque el envío a cocina falle.
        this.setBatch(this.batch().filter((l) => !lines.includes(l)));
        return toKitchen ? this.orderService.send(order.id, hold) : of(null);
      }),
    );

    const count = this.pendingCount();
    this.run(
      request$,
      toKitchen ? `Enviado a cocina (${count} plato${count === 1 ? '' : 's'})${hold ? '; platos fuertes y postres retenidos' : ''}.` : 'Apuntado en la cuenta.',
    );
    this.holdMains.set(false);
  }

  fire(course: OrderCourse): void {
    this.run(this.orderService.fire(this.orderId(), course), `Marchando: ${COURSE_LABELS[course].toLowerCase()}.`);
  }

  // ---------- Servir y anular ----------

  serve(item: OrderItemResponseDTO): void {
    this.run(this.orderService.setItemStatus(this.orderId(), item.id, 'Served', item.status), null);
  }

  readonly readyItems = computed(() => this.order()?.items.filter((i) => i.status === 'Ready') ?? []);

  serveAllReady(): void {
    const items = this.readyItems();
    if (!items.length) return;
    this.run(
      forkJoin(items.map((i) => this.orderService.setItemStatus(this.orderId(), i.id, 'Served', 'Ready'))),
      `${items.length} plato${items.length === 1 ? '' : 's'} servido${items.length === 1 ? '' : 's'}.`,
    );
  }

  /** Deshacer un "servido" tocado por error. */
  unserve(item: OrderItemResponseDTO): void {
    this.run(this.orderService.setItemStatus(this.orderId(), item.id, 'Ready', 'Served'), null);
  }

  cancelItem(item: OrderItemResponseDTO): void {
    const reason = prompt(`Motivo para anular "${item.name}" (queda registrado):`)?.trim();
    if (!reason) return;
    this.run(this.orderService.cancelItem(this.orderId(), item.id, reason), `"${item.name}" anulado.`);
  }

  // ---------- Mesa ----------

  changeGuests(delta: number): void {
    const order = this.order();
    if (!order) return;
    const guests = Math.min(50, Math.max(1, order.guests + delta));
    if (guests === order.guests) return;
    this.order.set({ ...order, guests });
    this.run(this.orderService.setGuests(order.id, guests), null);
  }

  startMove(): void {
    forkJoin({ tables: this.tableService.getTables(), open: this.orderService.getOpen() }).subscribe({
      next: ({ tables, open }) => {
        const busy = new Set(open.map((o) => o.tableId));
        this.freeTables.set(
          tables.filter((t) => !busy.has(t.id) && t.status !== 'OutOfService').sort((a, b) => a.tableNumber - b.tableNumber),
        );
        this.moving.set(true);
      },
      error: (err) => this.error.set(apiErrorMessage(err)),
    });
  }

  moveTo(table: TableResponseDTO): void {
    this.moving.set(false);
    this.run(this.orderService.moveToTable(this.orderId(), table.id), `Cuenta pasada a la mesa ${table.tableNumber}.`);
  }

  cancelOrder(): void {
    const reason = prompt('Motivo para anular toda la cuenta (queda registrado):')?.trim();
    if (!reason) return;
    this.busy.set(true);
    this.orderService.cancel(this.orderId(), reason).subscribe({
      next: () => {
        this.setBatch([]);
        this.router.navigate(['/sala']);
      },
      error: (err) => {
        this.busy.set(false);
        this.error.set(apiErrorMessage(err, 'No se pudo anular la cuenta.'));
      },
    });
  }

  // ---------- Ayudas ----------

  elapsed(iso: string | null | undefined): string {
    return formatElapsed(minutesSince(iso, this.now()));
  }

  allergenList(dish: { allergens: string[] }): string {
    return dish.allergens.map((a) => this.allergenLabels[a as keyof typeof ALLERGEN_LABELS] ?? a).join(', ');
  }

  private setBatch(lines: BatchLine[]): void {
    this.batch.set(lines);
    writeBatch(this.orderId(), lines);
  }

  /** Ejecuta una acción, muestra el aviso y recarga la cuenta. */
  private run(request$: Observable<unknown>, success: string | null): void {
    this.busy.set(true);
    this.error.set(null);
    request$.subscribe({
      next: () => {
        this.busy.set(false);
        if (success) this.notice.set(success);
        this.refresh();
      },
      error: (err) => {
        this.busy.set(false);
        this.error.set(apiErrorMessage(err, 'No se pudo completar la acción. Revisa la conexión e inténtalo otra vez.'));
        this.refresh();
      },
    });
  }
}

function normalize(text: string): string {
  return text.normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase().trim();
}

function readBatch(orderId: number): BatchLine[] {
  try {
    const raw = localStorage.getItem(BATCH_KEY(orderId));
    return raw ? (JSON.parse(raw) as BatchLine[]) : [];
  } catch {
    return [];
  }
}

function writeBatch(orderId: number, lines: BatchLine[]): void {
  try {
    if (lines.length) localStorage.setItem(BATCH_KEY(orderId), JSON.stringify(lines));
    else localStorage.removeItem(BATCH_KEY(orderId));
  } catch {
    // Sin almacenamiento: lo apuntado vive hasta recargar.
  }
}
