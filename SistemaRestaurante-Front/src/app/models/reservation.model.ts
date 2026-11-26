export type ReservationStatus = 'Pending' | 'Confirmed' | 'Completed' | 'Cancelled' | 'NoShow';

export interface MenuItemOrderDTO {
  name: string;
  quantity: number;
  unitPrice: number;
  subtotal: number;
}

export interface ReservationResponseDTO {
  id: number;
  /** Hora local del restaurante, formato ISO sin zona: "2026-11-29T20:00:00". */
  reservationDateTime: string;
  numberOfPeople: number;
  status: ReservationStatus;
  specialRequests?: string | null;
  totalAmount?: number | null;
  customerId: number;
  customerName: string;
  customerPhone: string;
  tableId: number;
  tableNumber: number;
  tableCapacity: number;
  orderedItems: MenuItemOrderDTO[];
  createdAt: string;
}

export interface ReservationRequestDTO {
  /** Formato "yyyy-MM-ddTHH:mm" (valor de un input datetime-local). */
  reservationDateTime: string;
  numberOfPeople: number;
  specialRequests?: string | null;
  customerId: number;
  tableId: number;
  menuItemIds?: number[];
  quantities?: number[];
}
