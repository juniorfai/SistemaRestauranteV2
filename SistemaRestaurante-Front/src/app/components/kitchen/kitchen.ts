import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { debounceTime, forkJoin } from 'rxjs';
import { KitchenTicketDTO, KitchenTicketItemDTO, OrderCourse, StationResponseDTO } from '../../models/order.model';
import { KitchenService } from '../../services/kitchen.service';
import { RealtimeService } from '../../services/realtime.service';
import { StationService } from '../../services/station.service';
import { apiErrorMessage } from '../../utils/api-error';
import { COURSES, COURSE_LABELS, ITEM_STATUS_LABELS } from '../../utils/labels';
import {
  Urgency,
  formatElapsed,
  minutesSince,
  newPendingItemIds,
  nextKitchenStatus,
  splitTickets,
  ticketKey,
  ticketStart,
  urgency,
} from '../../utils/order-utils';

const STATION_KEY = 'rm-kitchen-station';
const SOUND_KEY = 'rm-kitchen-sound';

/** Sin tiempo real se recarga cada 15 s; con él, solo se actualizan los minutos. */
const TICK_MS = 15_000;

interface TicketView {
  key: string;
  ticket: KitchenTicketDTO;
  minutes: number;
  urgency: Urgency;
  courses: { course: OrderCourse; items: KitchenTicketItemDTO[] }[];
}

/** Pantalla de cocina y barra: comandas pendientes, de la más antigua a la más nueva. */
@Component({
  selector: 'app-kitchen',
  standalone: true,
  templateUrl: './kitchen.html',
  styleUrl: './kitchen.css',
})
export class KitchenComponent implements OnInit {
  private readonly kitchen = inject(KitchenService);
  private readonly stationService = inject(StationService);
  readonly realtime = inject(RealtimeService);

  readonly courseLabels = COURSE_LABELS;
  readonly statusLabels = ITEM_STATUS_LABELS;

  readonly stations = signal<StationResponseDTO[]>([]);
  readonly stationId = signal<number | null>(readNumber(STATION_KEY));
  readonly tickets = signal<KitchenTicketDTO[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly now = signal(Date.now());
  readonly busy = signal<ReadonlySet<string>>(new Set());
  readonly soundOn = signal(readFlag(SOUND_KEY));
  readonly showDone = signal(false);
  readonly fullscreen = signal(false);

  private readonly split = computed(() => splitTickets(this.tickets()));
  readonly active = computed<TicketView[]>(() => this.split().active.map((t) => this.view(t)));
  readonly done = computed(() => this.split().done);
  readonly pendingDishes = computed(() =>
    this.split().active.reduce(
      (n, t) => n + t.items.filter((i) => i.status === 'Sent' || i.status === 'Preparing').reduce((m, i) => m + i.quantity, 0),
      0,
    ),
  );

  private seen = new Set<number>();
  private firstLoad = true;
  private audio: AudioContext | null = null;

  constructor() {
    const destroyRef = inject(DestroyRef);
    this.realtime.orderChanged$.pipe(debounceTime(300), takeUntilDestroyed()).subscribe(() => this.load());
    this.realtime.resync$.pipe(takeUntilDestroyed()).subscribe(() => this.load());

    const timer = setInterval(() => {
      this.now.set(Date.now());
      if (this.realtime.status() !== 'online' && !document.hidden) this.load();
    }, TICK_MS);

    const onFullscreen = () => this.fullscreen.set(!!document.fullscreenElement);
    document.addEventListener('fullscreenchange', onFullscreen);
    // El navegador solo deja sonar audio después de que el usuario toque la pantalla.
    const unlock = () => this.ensureAudio();
    document.addEventListener('pointerdown', unlock, { once: true });

    destroyRef.onDestroy(() => {
      clearInterval(timer);
      document.removeEventListener('fullscreenchange', onFullscreen);
      document.removeEventListener('pointerdown', unlock);
      this.audio?.close().catch(() => undefined);
    });
  }

  ngOnInit(): void {
    forkJoin({ stations: this.stationService.getAll(), tickets: this.kitchen.getTickets(this.stationId()) }).subscribe({
      next: ({ stations, tickets }) => {
        this.stations.set([...stations].sort((a, b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name)));
        // La zona guardada ya no existe: se muestran todas.
        if (this.stationId() && !stations.some((s) => s.id === this.stationId())) {
          this.selectStation(null);
          return;
        }
        this.apply(tickets);
      },
      error: (err) => this.fail(err, 'No se pudo cargar la cocina.'),
    });
  }

  load(): void {
    this.kitchen.getTickets(this.stationId()).subscribe({
      next: (tickets) => this.apply(tickets),
      error: (err) => this.fail(err, 'No se pudieron cargar las comandas.'),
    });
  }

  selectStation(id: number | null): void {
    this.stationId.set(id);
    writeValue(STATION_KEY, id === null ? null : String(id));
    this.firstLoad = true;
    this.loading.set(true);
    this.load();
  }

  /** Tocar un plato lo avanza (enviado → preparando → listo); tocarlo listo lo devuelve. */
  advance(ticket: KitchenTicketDTO, item: KitchenTicketItemDTO): void {
    const next = nextKitchenStatus(item.status);
    const key = `item-${item.id}`;
    if (!next || this.busy().has(key)) return;
    this.setBusy(key, true);
    this.patchItems(ticket, (i) => (i.id === item.id ? { ...i, status: next } : i));
    this.kitchen.setItemStatus(ticket.orderId, item.id, next, item.status).subscribe({
      next: () => this.setBusy(key, false),
      error: (err) => {
        this.setBusy(key, false);
        this.fail(err, 'No se pudo cambiar el plato.');
        this.load();
      },
    });
  }

  /** Todo lo pendiente de la comanda queda listo. */
  bump(ticket: KitchenTicketDTO): void {
    const key = ticketKey(ticket);
    if (this.busy().has(key)) return;
    this.setBusy(key, true);
    this.patchItems(ticket, (i) => (i.status === 'Sent' || i.status === 'Preparing' ? { ...i, status: 'Ready' } : i));
    this.kitchen.bump(ticket.orderId, ticket.stationId).subscribe({
      next: () => this.setBusy(key, false),
      error: (err) => {
        this.setBusy(key, false);
        this.fail(err, 'No se pudo despachar la comanda.');
        this.load();
      },
    });
  }

  /** Recupera una comanda despachada por error: sus platos listos vuelven a preparación. */
  recall(ticket: KitchenTicketDTO): void {
    const key = ticketKey(ticket);
    if (this.busy().has(key)) return;
    const ready = ticket.items.filter((i) => i.status === 'Ready');
    this.setBusy(key, true);
    this.patchItems(ticket, (i) => (i.status === 'Ready' ? { ...i, status: 'Preparing' } : i));
    forkJoin(ready.map((i) => this.kitchen.setItemStatus(ticket.orderId, i.id, 'Preparing', 'Ready'))).subscribe({
      next: () => this.setBusy(key, false),
      error: (err) => {
        this.setBusy(key, false);
        this.fail(err, 'No se pudo recuperar la comanda.');
        this.load();
      },
    });
  }

  isBusy(key: string): boolean {
    return this.busy().has(key);
  }

  toggleSound(): void {
    const on = !this.soundOn();
    this.soundOn.set(on);
    writeValue(SOUND_KEY, on ? '1' : null);
    if (on) {
      this.ensureAudio();
      this.beep();
    }
  }

  toggleFullscreen(): void {
    if (document.fullscreenElement) document.exitFullscreen().catch(() => undefined);
    else document.documentElement.requestFullscreen?.().catch(() => undefined);
  }

  readySummary(ticket: KitchenTicketDTO): string {
    return ticket.items
      .filter((i) => i.status === 'Ready')
      .map((i) => `${i.quantity} ${i.name}`)
      .join(', ');
  }

  elapsedLabel(minutes: number): string {
    return formatElapsed(minutes);
  }

  // ---------- Internos ----------

  private apply(tickets: KitchenTicketDTO[]): void {
    const fresh = newPendingItemIds(tickets, this.seen);
    if (!this.firstLoad && fresh.length > 0 && this.soundOn()) this.beep();
    for (const t of tickets) for (const i of t.items) this.seen.add(i.id);
    this.firstLoad = false;
    this.tickets.set(tickets);
    this.now.set(Date.now());
    this.loading.set(false);
    this.error.set(null);
  }

  private view(ticket: KitchenTicketDTO): TicketView {
    const minutes = minutesSince(ticketStart(ticket), this.now());
    return {
      key: ticketKey(ticket),
      ticket,
      minutes,
      urgency: urgency(minutes),
      courses: COURSES.map((course) => ({ course, items: ticket.items.filter((i) => i.course === course) })).filter(
        (g) => g.items.length > 0,
      ),
    };
  }

  private patchItems(ticket: KitchenTicketDTO, change: (i: KitchenTicketItemDTO) => KitchenTicketItemDTO): void {
    const key = ticketKey(ticket);
    this.tickets.update((list) => list.map((t) => (ticketKey(t) === key ? { ...t, items: t.items.map(change) } : t)));
  }

  private setBusy(key: string, on: boolean): void {
    this.busy.update((set) => {
      const next = new Set(set);
      if (on) next.add(key);
      else next.delete(key);
      return next;
    });
  }

  private fail(err: unknown, fallback: string): void {
    this.loading.set(false);
    const conflict = err instanceof HttpErrorResponse && err.status === 409;
    this.error.set(conflict ? 'Otra pantalla ya había cambiado esa comanda; se ha actualizado.' : apiErrorMessage(err, fallback));
  }

  private ensureAudio(): void {
    if (!this.soundOn()) return;
    try {
      this.audio ??= new AudioContext();
      if (this.audio.state === 'suspended') this.audio.resume().catch(() => undefined);
    } catch {
      this.audio = null;
    }
  }

  /** Dos pitidos cortos: llegó una comanda nueva. */
  private beep(): void {
    const ctx = this.audio;
    if (!ctx || ctx.state !== 'running') return;
    [0, 0.22].forEach((offset) => {
      const osc = ctx.createOscillator();
      const gain = ctx.createGain();
      osc.frequency.value = 880;
      gain.gain.setValueAtTime(0.0001, ctx.currentTime + offset);
      gain.gain.exponentialRampToValueAtTime(0.3, ctx.currentTime + offset + 0.02);
      gain.gain.exponentialRampToValueAtTime(0.0001, ctx.currentTime + offset + 0.18);
      osc.connect(gain).connect(ctx.destination);
      osc.start(ctx.currentTime + offset);
      osc.stop(ctx.currentTime + offset + 0.2);
    });
  }
}

function readNumber(key: string): number | null {
  try {
    const value = Number(localStorage.getItem(key));
    return Number.isInteger(value) && value > 0 ? value : null;
  } catch {
    return null;
  }
}

function readFlag(key: string): boolean {
  try {
    return localStorage.getItem(key) === '1';
  } catch {
    return false;
  }
}

function writeValue(key: string, value: string | null): void {
  try {
    if (value === null) localStorage.removeItem(key);
    else localStorage.setItem(key, value);
  } catch {
    // Sin almacenamiento: la preferencia dura hasta recargar.
  }
}
