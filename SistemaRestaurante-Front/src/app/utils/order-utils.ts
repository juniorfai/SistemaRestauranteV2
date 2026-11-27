import { Allergen, MenuItemCategory, MenuItemResponseDTO } from '../models/menu.model';
import { AddOrderItemDTO, OrderCourse, OrderItemResponseDTO, OrderResponseDTO } from '../models/order.model';
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
