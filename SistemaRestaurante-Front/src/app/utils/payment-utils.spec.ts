import { OrderItemResponseDTO, OrderResponseDTO } from '../models/order.model';
import { blockingItems, changeDue, quickCashAmounts, summarizeDay, tipFor } from './order-utils';

const order = (over: Partial<OrderResponseDTO>): OrderResponseDTO => ({
  id: 1,
  tableId: 1,
  tableNumber: 1,
  waiterId: 1,
  waiterName: 'Pablo',
  guests: 2,
  status: 'Closed',
  openedAt: '2026-11-27T12:00:00',
  subtotal: 0,
  items: [],
  ...over,
});

const item = (status: OrderItemResponseDTO['status'], quantity = 1): OrderItemResponseDTO => ({
  id: 1,
  menuItemId: 1,
  name: 'Plato',
  unitPrice: 10000,
  quantity,
  lineTotal: 10000 * quantity,
  course: 'Main',
  status,
  stationId: 1,
  stationName: 'Cocina',
  createdAt: '2026-11-27T12:00:00',
});

describe('cobro', () => {
  it('cobra la propina que elige el cliente', () => {
    expect(tipFor('suggested', 6200, 999)).toBe(6200);
    expect(tipFor('none', 6200, 999)).toBe(0);
    expect(tipFor('custom', 6200, 5000)).toBe(5000);
    expect(tipFor('custom', 6200, 4999.6)).toBe(5000);
    expect(tipFor('custom', 6200, -100)).toBe(0);
    expect(tipFor('custom', 6200, null)).toBe(0);
  });

  it('calcula las vueltas solo si alcanza', () => {
    expect(changeDue(68200, 100000)).toBe(31800);
    expect(changeDue(68200, 68200)).toBe(0);
    expect(changeDue(68200, 50000)).toBeNull();
    expect(changeDue(68200, null)).toBeNull();
  });

  it('propone billetes redondos para pagar en efectivo', () => {
    expect(quickCashAmounts(68200)).toEqual([68200, 70000, 80000, 100000]);
    expect(quickCashAmounts(50000)).toEqual([50000, 60000, 100000]);
  });

  it('no deja cerrar con platos en cocina o retenidos, pero sí con platos listos', () => {
    expect(blockingItems(order({ items: [item('Ready', 2), item('Served')] }))).toBe(0);
    expect(blockingItems(order({ items: [item('Sent', 2), item('Preparing'), item('Held'), item('Cancelled')] }))).toBe(4);
  });

  it('resume la caja del día por forma de pago, con las propinas aparte', () => {
    const s = summarizeDay([
      order({ subtotal: 62000, tipAmount: 6200, paymentMethod: 'Card', guests: 2 }),
      order({ subtotal: 30000, tipAmount: 0, paymentMethod: 'Cash', guests: 1 }),
      order({ subtotal: 20000, tipAmount: 2000, paymentMethod: 'Card', guests: 3 }),
      order({ status: 'Cancelled', subtotal: 0, guests: 4 }),
    ]);
    expect(s.closed).toBe(3);
    expect(s.cancelled).toBe(1);
    expect(s.guests).toBe(6);
    expect(s.sales).toBe(112000);
    expect(s.tips).toBe(8200);
    expect(s.byMethod).toEqual([
      { method: 'Card', orders: 2, sales: 82000, tips: 8200 },
      { method: 'Cash', orders: 1, sales: 30000, tips: 0 },
    ]);
  });
});
