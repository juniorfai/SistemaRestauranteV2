import { Allergen, MenuItemCategory, MenuItemResponseDTO } from '../models/menu.model';
import {
  AddOrderItemDTO,
  KitchenTicketDTO,
  OrderCourse,
  OrderItemResponseDTO,
  OrderItemStatus,
  OrderResponseDTO,
  PaymentMethod,
} from '../models/order.model';
import { COURSES, DEFAULT_COURSE } from './labels';

/** Plato apuntado en la tableta que aún no ha llegado a la API. */
export interface BatchLine {
  /** También es el clientRequestId: si el envío se repite, la API no lo duplica. */
  key: string;
  menuItemId: number;
  name: string;
  unitPrice: number;
  category: MenuItemCategory;
  allergens: Allergen[];
  quantity: number;
  notes: string;
  course: OrderCourse;
}

export function newRequestId(): string {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID();
  }
  // Navegadores antiguos (o http sin TLS): UUID v4 con Math.random.
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
    const r = (Math.random() * 16) | 0;
    return (c === 'x' ? r : (r & 0x3) | 0x8).toString(16);
  });
}

/**
 * Añade un plato a lo apuntado. Si ya hay una línea igual sin notas, suma uno; las líneas con
 * notas ("sin cebolla") se quedan aparte para que cocina las vea separadas.
 */
export function addToBatch(batch: BatchLine[], dish: MenuItemResponseDTO): BatchLine[] {
  const same = batch.find((l) => l.menuItemId === dish.id && !l.notes.trim());
  if (same) {
    return batch.map((l) => (l === same ? { ...l, quantity: Math.min(99, l.quantity + 1) } : l));
  }
  return [
    ...batch,
    {
      key: newRequestId(),
      menuItemId: dish.id,
      name: dish.name,
      unitPrice: dish.price,
      category: dish.category,
      allergens: dish.allergens ?? [],
      quantity: 1,
      notes: '',
      course: DEFAULT_COURSE[dish.category],
    },
  ];
}

export function toAddItems(batch: BatchLine[]): AddOrderItemDTO[] {
  return batch.map((l) => ({
    menuItemId: l.menuItemId,
    quantity: l.quantity,
    notes: l.notes.trim() || null,
    course: l.course,
    clientRequestId: l.key,
  }));
}

export function batchTotal(batch: BatchLine[]): number {
  return batch.reduce((sum, l) => sum + l.unitPrice * l.quantity, 0);
}

/** Resumen para la tarjeta de la mesa en el plano de sala. */
export interface OrderSummary {
  drafts: number;
  held: number;
  inKitchen: number;
  ready: number;
  served: number;
}

export function summarize(order: OrderResponseDTO): OrderSummary {
  const sum = (pred: (i: OrderItemResponseDTO) => boolean) =>
    order.items.filter(pred).reduce((n, i) => n + i.quantity, 0);
  return {
    drafts: sum((i) => i.status === 'Draft'),
    held: sum((i) => i.status === 'Held'),
    inKitchen: sum((i) => i.status === 'Sent' || i.status === 'Preparing'),
    ready: sum((i) => i.status === 'Ready'),
    served: sum((i) => i.status === 'Served'),
  };
}

/** Platos ya enviados (o retenidos), agrupados por tiempo en el orden del servicio. */
export function groupByCourse(items: OrderItemResponseDTO[]): { course: OrderCourse; items: OrderItemResponseDTO[] }[] {
  return COURSES.map((course) => ({ course, items: items.filter((i) => i.course === course) })).filter(
    (g) => g.items.length > 0,
  );
}

/** Tiempos con platos retenidos, para los botones de "marchar". */
export function heldCourses(order: OrderResponseDTO): OrderCourse[] {
  return COURSES.filter((c) => order.items.some((i) => i.status === 'Held' && i.course === c));
}

/** Minutos desde una fecha UTC de la API. */
export function minutesSince(isoUtc: string | null | undefined, now = Date.now()): number {
  if (!isoUtc) return 0;
  const hasZone = /[zZ]|[+-]\d{2}:?\d{2}$/.test(isoUtc);
  const time = new Date(hasZone ? isoUtc : `${isoUtc}Z`).getTime();
  return Math.max(0, Math.floor((now - time) / 60000));
}

/** "8 min", "1 h 05". */
export function formatElapsed(minutes: number): string {
  if (minutes < 60) return `${minutes} min`;
  const h = Math.floor(minutes / 60);
  const m = minutes % 60;
  return `${h} h ${m.toString().padStart(2, '0')}`;
}

// ---------- Pantalla de cocina ----------

/** Minutos a partir de los que una comanda se marca en ámbar y en rojo. */
export const KITCHEN_WARN_MIN = 10;
export const KITCHEN_LATE_MIN = 20;

export type Urgency = 'ok' | 'warn' | 'late';

export function urgency(minutes: number): Urgency {
  if (minutes >= KITCHEN_LATE_MIN) return 'late';
  if (minutes >= KITCHEN_WARN_MIN) return 'warn';
  return 'ok';
}

/** Al tocar un plato: enviado → preparando → listo; si ya estaba listo, vuelve a preparando. */
export function nextKitchenStatus(status: OrderItemStatus): OrderItemStatus | null {
  switch (status) {
    case 'Sent': return 'Preparing';
    case 'Preparing': return 'Ready';
    case 'Ready': return 'Preparing';
    default: return null;
  }
}

const isPending = (s: OrderItemStatus) => s === 'Sent' || s === 'Preparing';

export function ticketKey(t: Pick<KitchenTicketDTO, 'orderId' | 'stationId'>): string {
  return `${t.orderId}-${t.stationId}`;
}

/** Desde cuándo espera lo pendiente de la comanda (lo retenido no cuenta). */
export function ticketStart(t: KitchenTicketDTO): string {
  const sent = t.items.filter((i) => isPending(i.status)).map((i) => i.sentAt).filter((d): d is string => !!d);
  return sent.length ? sent.reduce((a, b) => (a < b ? a : b)) : t.firstSentAt;
}

/**
 * Separa lo que hay que preparar (la más antigua primero) de lo ya despachado que espera a que
 * sala lo recoja (por si hay que recuperarlo). Las comandas solo con platos retenidos no salen.
 */
export function splitTickets(tickets: KitchenTicketDTO[]): { active: KitchenTicketDTO[]; done: KitchenTicketDTO[] } {
  const active = tickets
    .filter((t) => t.items.some((i) => isPending(i.status)))
    .sort((a, b) => ticketStart(a).localeCompare(ticketStart(b)) || a.orderId - b.orderId);
  const done = tickets
    .filter((t) => !t.items.some((i) => isPending(i.status)) && t.items.some((i) => i.status === 'Ready'))
    .sort((a, b) => b.orderId - a.orderId);
  return { active, done };
}

/** Platos que acaban de llegar a cocina (para el aviso sonoro). */
export function newPendingItemIds(tickets: KitchenTicketDTO[], seen: ReadonlySet<number>): number[] {
  return tickets.flatMap((t) => t.items).filter((i) => i.status === 'Sent' && !seen.has(i.id)).map((i) => i.id);
}

// ---------- Cobro ----------

export type TipChoice = 'suggested' | 'none' | 'custom';

/** Propina a cobrar según lo que elija el cliente (la otra cantidad, redondeada y nunca negativa). */
export function tipFor(choice: TipChoice, suggestedTip: number, custom: number | null): number {
  if (choice === 'suggested') return suggestedTip;
  if (choice === 'none') return 0;
  const value = Number(custom);
  return Number.isFinite(value) && value > 0 ? Math.round(value) : 0;
}

/** Vueltas en efectivo; null si aún no se sabe cuánto entregó o no alcanza. */
export function changeDue(total: number, received: number | null): number | null {
  if (received === null) return null;
  const value = Number(received);
  if (!Number.isFinite(value) || value < total) return null;
  return value - total;
}

/** Billetes con los que suele pagarse una cuenta, para tocar en vez de teclear. */
export function quickCashAmounts(total: number): number[] {
  const options = new Set<number>([total]);
  for (const step of [10_000, 20_000, 50_000, 100_000]) options.add(Math.ceil(total / step) * step);
  return [...options].filter((n) => n >= total).sort((a, b) => a - b).slice(0, 4);
}

/** Platos que aún bloquean el cierre: retenidos o en cocina (lo listo se da por servido). */
export function blockingItems(order: OrderResponseDTO): number {
  return order.items
    .filter((i) => i.status === 'Held' || i.status === 'Sent' || i.status === 'Preparing')
    .reduce((n, i) => n + i.quantity, 0);
}

export interface PaymentTotals {
  method: PaymentMethod;
  orders: number;
  sales: number;
  tips: number;
}

export interface DaySummary {
  closed: number;
  cancelled: number;
  guests: number;
  sales: number;
  tips: number;
  byMethod: PaymentTotals[];
}

/** Caja del día: ventas (sin propina), propinas y desglose por forma de pago. */
export function summarizeDay(orders: OrderResponseDTO[]): DaySummary {
  const closed = orders.filter((o) => o.status === 'Closed');
  const byMethod = new Map<PaymentMethod, PaymentTotals>();
  for (const o of closed) {
    const method = o.paymentMethod ?? 'Other';
    const row = byMethod.get(method) ?? { method, orders: 0, sales: 0, tips: 0 };
    row.orders++;
    row.sales += o.subtotal;
    row.tips += o.tipAmount ?? 0;
    byMethod.set(method, row);
  }
  return {
    closed: closed.length,
    cancelled: orders.filter((o) => o.status === 'Cancelled').length,
    guests: closed.reduce((n, o) => n + o.guests, 0),
    sales: closed.reduce((n, o) => n + o.subtotal, 0),
    tips: closed.reduce((n, o) => n + (o.tipAmount ?? 0), 0),
    byMethod: [...byMethod.values()].sort((a, b) => b.sales + b.tips - (a.sales + a.tips)),
  };
}

function parseUtc(isoUtc: string): Date {
  const hasZone = /[zZ]|[+-]\d{2}:?\d{2}$/.test(isoUtc);
  return new Date(hasZone ? isoUtc : `${isoUtc}Z`);
}

/** Hora local (HH:mm) de una fecha UTC de la API. */
export function localTime(isoUtc: string | null | undefined): string {
  if (!isoUtc) return '';
  return parseUtc(isoUtc).toLocaleTimeString('es-CO', { hour: '2-digit', minute: '2-digit', hour12: false });
}

/** Fecha y hora local para imprimir. */
export function localDateTime(isoUtc: string | null | undefined): string {
  if (!isoUtc) return '';
  return parseUtc(isoUtc).toLocaleString('es-CO', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
  });
}
