export type OrderStatus = 'Open' | 'Closed' | 'Cancelled';
export type OrderItemStatus = 'Draft' | 'Held' | 'Sent' | 'Preparing' | 'Ready' | 'Served' | 'Cancelled';
export type OrderCourse = 'Immediate' | 'Starter' | 'Main' | 'Dessert';
export type PaymentMethod = 'Cash' | 'Card' | 'Transfer' | 'Other';
export type StationType = 'Kitchen' | 'Bar' | 'Other';

export interface StationResponseDTO {
  id: number;
  name: string;
  type: StationType;
  sortOrder: number;
  printerAddress?: string | null;
}

export interface StationRequestDTO {
  name: string;
  type: StationType;
  sortOrder: number;
  printerAddress?: string | null;
}

export interface OrderItemResponseDTO {
  id: number;
  menuItemId: number;
  name: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
  notes?: string | null;
  course: OrderCourse;
  status: OrderItemStatus;
  stationId: number;
  stationName: string;
  clientRequestId?: string | null;
  createdAt: string;
  sentAt?: string | null;
  startedAt?: string | null;
  readyAt?: string | null;
  servedAt?: string | null;
  cancelledAt?: string | null;
  cancelReason?: string | null;
}

export interface OrderResponseDTO {
  id: number;
  tableId: number;
  tableNumber: number;
  waiterId: number;
  waiterName: string;
  reservationId?: number | null;
  guests: number;
  notes?: string | null;
  status: OrderStatus;
  /** UTC en ISO. */
  openedAt: string;
  closedAt?: string | null;
  /** Abierta: lo pedido (sin borradores ni anulados). Cerrada: lo cobrado. */
  subtotal: number;
  tipAmount?: number | null;
  paymentMethod?: PaymentMethod | null;
  cancelReason?: string | null;
  items: OrderItemResponseDTO[];
}

export interface OpenOrderRequestDTO {
  tableId: number;
  guests: number;
  reservationId?: number | null;
  notes?: string | null;
}

export interface AddOrderItemDTO {
  menuItemId: number;
  quantity: number;
  notes?: string | null;
  course?: OrderCourse | null;
  /** Lo genera la tableta: si el envío se repite, el plato no se duplica. */
  clientRequestId: string;
}

export interface UpdateOrderItemDTO {
  quantity: number;
  notes?: string | null;
  course: OrderCourse;
}

// ---------- Pantalla de cocina ----------

export interface KitchenTicketItemDTO {
  id: number;
  name: string;
  quantity: number;
  notes?: string | null;
  course: OrderCourse;
  /** Held (espera a que el camarero lo marche), Sent, Preparing o Ready. */
  status: OrderItemStatus;
  sentAt?: string | null;
}

/** Comanda de una zona: los platos de una cuenta que prepara esa zona. */
export interface KitchenTicketDTO {
  orderId: number;
  tableNumber: number;
  waiterName: string;
  guests: number;
  stationId: number;
  stationName: string;
  /** UTC en ISO. */
  firstSentAt: string;
  items: KitchenTicketItemDTO[];
}

// ---------- Cuenta y cobro ----------

export interface BillLineDTO {
  name: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

/** Cuenta provisional (no es factura). En una cuenta cerrada trae también lo cobrado. */
export interface BillResponseDTO {
  restaurantName: string;
  restaurantAddress: string;
  restaurantPhone: string;
  orderId: number;
  status: OrderStatus;
  tableNumber: number;
  guests: number;
  waiterName: string;
  openedAt: string;
  currencyCode: string;
  lines: BillLineDTO[];
  subtotal: number;
  suggestedTipPercent: number;
  suggestedTip: number;
  totalWithSuggestedTip: number;
  pendingItems: number;
  tipAmount?: number | null;
  paymentMethod?: PaymentMethod | null;
  closedAt?: string | null;
}

export interface CloseOrderRequestDTO {
  paymentMethod: PaymentMethod;
  tipAmount: number;
}
