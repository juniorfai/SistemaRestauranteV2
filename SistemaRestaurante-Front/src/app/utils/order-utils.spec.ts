import { MenuItemResponseDTO } from '../models/menu.model';
import { OrderItemResponseDTO, OrderResponseDTO } from '../models/order.model';
import { formatMoney } from '../services/money.service';
import {
  addToBatch,
  batchTotal,
  formatElapsed,
  groupByCourse,
  heldCourses,
  minutesSince,
  newRequestId,
  summarize,
  toAddItems,
} from './order-utils';

const dish = (id: number, category: MenuItemResponseDTO['category'], price = 10000): MenuItemResponseDTO => ({
  id,
  name: `Plato ${id}`,
  price,
  category,
  categoryName: category,
  isAvailable: true,
  restaurantId: 1,
  stationId: 1,
  stationName: 'Cocina',
  allergens: [],
});

const item = (id: number, status: OrderItemResponseDTO['status'], course: OrderItemResponseDTO['course'], quantity = 1): OrderItemResponseDTO => ({
  id,
  menuItemId: id,
  name: `Plato ${id}`,
  unitPrice: 10000,
  quantity,
  lineTotal: 10000 * quantity,
  course,
  status,
  stationId: 1,
  stationName: 'Cocina',
  createdAt: '2026-11-27T12:00:00',
});

const order = (items: OrderItemResponseDTO[]): OrderResponseDTO => ({
  id: 1,
  tableId: 1,
  tableNumber: 4,
  waiterId: 7,
  waiterName: 'Pablo',
  guests: 2,
  status: 'Open',
  openedAt: '2026-11-27T12:00:00',
  subtotal: 0,
  items,
});

describe('order-utils', () => {
  it('suma el mismo plato sin notas y separa el que lleva notas', () => {
    let batch = addToBatch([], dish(1, 'MainCourse'));
    batch = addToBatch(batch, dish(1, 'MainCourse'));
    expect(batch.length).toBe(1);
    expect(batch[0].quantity).toBe(2);

    batch = batch.map((l) => ({ ...l, notes: 'sin cebolla' }));
    batch = addToBatch(batch, dish(1, 'MainCourse'));
    expect(batch.length).toBe(2);
    expect(batch[1].quantity).toBe(1);
  });

  it('pone el tiempo según la categoría y cada línea lleva su propio id de envío', () => {
    let batch = addToBatch([], dish(1, 'Drink', 6000));
    batch = addToBatch(batch, dish(2, 'Appetizer'));
    batch = addToBatch(batch, dish(3, 'Dessert'));
    expect(batch.map((l) => l.course)).toEqual(['Immediate', 'Starter', 'Dessert']);

    const items = toAddItems(batch);
    expect(new Set(items.map((i) => i.clientRequestId)).size).toBe(3);
    expect(items[0].notes).toBeNull();
    expect(batchTotal(batch)).toBe(26000);
  });

  it('genera identificadores con formato UUID', () => {
    expect(newRequestId()).toMatch(/^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/);
  });

  it('resume la mesa para el plano de sala', () => {
    const s = summarize(order([
      item(1, 'Draft', 'Main', 2),
      item(2, 'Sent', 'Main'),
      item(3, 'Preparing', 'Main'),
      item(4, 'Ready', 'Starter', 3),
      item(5, 'Held', 'Dessert'),
      item(6, 'Served', 'Immediate'),
      item(7, 'Cancelled', 'Main'),
    ]));
    expect(s).toEqual({ drafts: 2, held: 1, inKitchen: 2, ready: 3, served: 1 });
  });

  it('agrupa por tiempo en el orden del servicio y detecta lo retenido', () => {
    const o = order([item(1, 'Sent', 'Dessert'), item(2, 'Held', 'Main'), item(3, 'Served', 'Immediate'), item(4, 'Held', 'Dessert')]);
    expect(groupByCourse(o.items).map((g) => g.course)).toEqual(['Immediate', 'Main', 'Dessert']);
    expect(heldCourses(o)).toEqual(['Main', 'Dessert']);
  });

  it('cuenta el tiempo desde fechas UTC con o sin zona', () => {
    const now = Date.parse('2026-11-27T12:45:00Z');
    expect(minutesSince('2026-11-27T12:00:00', now)).toBe(45);
    expect(minutesSince('2026-11-27T12:00:00Z', now)).toBe(45);
    expect(minutesSince(null, now)).toBe(0);
    expect(formatElapsed(8)).toBe('8 min');
    expect(formatElapsed(65)).toBe('1 h 05');
  });

  it('formatea pesos colombianos sin decimales', () => {
    const cop = formatMoney(25000, 'COP').replace(/\s/g, ' ');
    expect(cop).toContain('25.000');
    expect(cop).not.toContain(',');
    expect(formatMoney(12.5, 'USD')).toContain('12,50');
    expect(formatMoney(null)).toBe('—');
  });
});
