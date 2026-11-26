import { MenuItemCategory } from '../models/menu.model';
import { ReservationStatus } from '../models/reservation.model';
import { TableStatus } from '../models/table.model';

export const RESERVATION_STATUS_LABELS: Record<ReservationStatus, string> = {
  Pending: 'Pendiente',
  Confirmed: 'Confirmada',
  Completed: 'Completada',
  Cancelled: 'Cancelada',
  NoShow: 'No asistió',
};

export const RESERVATION_STATUS_BADGES: Record<ReservationStatus, string> = {
  Pending: 'bg-warning text-dark',
  Confirmed: 'bg-primary',
  Completed: 'bg-success',
  Cancelled: 'bg-secondary',
  NoShow: 'bg-danger',
};

export const TABLE_STATUS_LABELS: Record<TableStatus, string> = {
  Available: 'Disponible',
  Occupied: 'Ocupada',
  Reserved: 'Reservada',
  OutOfService: 'Fuera de servicio',
};

export const TABLE_STATUS_BADGES: Record<TableStatus, string> = {
  Available: 'bg-success',
  Occupied: 'bg-danger',
  Reserved: 'bg-primary',
  OutOfService: 'bg-secondary',
};

export const TABLE_STATUSES = Object.keys(TABLE_STATUS_LABELS) as TableStatus[];

export const MENU_CATEGORY_LABELS: Record<MenuItemCategory, string> = {
  Appetizer: 'Entradas',
  MainCourse: 'Platos fuertes',
  SideDish: 'Acompañamientos',
  Dessert: 'Postres',
  Drink: 'Bebidas',
};

export const MENU_CATEGORIES = Object.keys(MENU_CATEGORY_LABELS) as MenuItemCategory[];

/** Fecha local en formato yyyy-MM-dd (para inputs type="date"). */
export function toDateInputValue(date: Date): string {
  const pad = (n: number) => n.toString().padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}
