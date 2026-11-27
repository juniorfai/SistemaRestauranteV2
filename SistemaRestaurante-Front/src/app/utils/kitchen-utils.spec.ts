import { KitchenTicketDTO, KitchenTicketItemDTO, OrderItemStatus } from '../models/order.model';
import { retryDelay } from '../services/realtime.service';
import { newPendingItemIds, nextKitchenStatus, splitTickets, ticketStart, urgency } from './order-utils';

const item = (id: number, status: OrderItemStatus, sentAt: string | null = '2026-11-27T12:00:00'): KitchenTicketItemDTO => ({
  id,
  name: `Plato ${id}`,
  quantity: 1,
  course: 'Main',
  status,
  sentAt,
});

const ticket = (orderId: number, items: KitchenTicketItemDTO[], firstSentAt = '2026-11-27T11:00:00'): KitchenTicketDTO => ({
  orderId,
  tableNumber: orderId,
  waiterName: 'Pablo',
  guests: 2,
  stationId: 1,
  stationName: 'Cocina',
  firstSentAt,
  items,
});

describe('pantalla de cocina', () => {
  it('avanza el plato al tocarlo y deshace lo listo', () => {
    expect(nextKitchenStatus('Sent')).toBe('Preparing');
    expect(nextKitchenStatus('Preparing')).toBe('Ready');
    expect(nextKitchenStatus('Ready')).toBe('Preparing');
    expect(nextKitchenStatus('Held')).toBeNull();
  });

  it('colorea la espera a los 10 y a los 20 minutos', () => {
    expect(urgency(9)).toBe('ok');
    expect(urgency(10)).toBe('warn');
    expect(urgency(19)).toBe('warn');
    expect(urgency(20)).toBe('late');
  });

  it('ordena lo pendiente de lo más antiguo a lo más nuevo y aparta lo despachado', () => {
    const late = ticket(1, [item(1, 'Sent', '2026-11-27T12:30:00')]);
    const early = ticket(2, [item(2, 'Preparing', '2026-11-27T12:05:00'), item(3, 'Held', null)]);
    const dispatched = ticket(3, [item(4, 'Ready')]);
    const onlyHeld = ticket(4, [item(5, 'Held', null)]);

    const { active, done } = splitTickets([late, dispatched, early, onlyHeld]);
    expect(active.map((t) => t.orderId)).toEqual([2, 1]);
    expect(done.map((t) => t.orderId)).toEqual([3]);
  });

  it('cuenta la espera desde lo pendiente, no desde lo ya servido o retenido', () => {
    const t = ticket(1, [item(1, 'Ready', '2026-11-27T11:30:00'), item(2, 'Sent', '2026-11-27T12:10:00')]);
    expect(ticketStart(t)).toBe('2026-11-27T12:10:00');
    expect(ticketStart(ticket(2, [item(3, 'Held', null)]))).toBe('2026-11-27T11:00:00');
  });

  it('detecta los platos recién llegados para el aviso sonoro', () => {
    const tickets = [ticket(1, [item(1, 'Sent'), item(2, 'Preparing')]), ticket(2, [item(3, 'Sent')])];
    expect(newPendingItemIds(tickets, new Set([1]))).toEqual([3]);
  });

  it('reintenta la conexión rápido al principio y luego cada 15 s', () => {
    expect([0, 1, 2, 3].map(retryDelay)).toEqual([0, 2000, 5000, 10000]);
    expect(retryDelay(50)).toBe(15000);
  });
});
