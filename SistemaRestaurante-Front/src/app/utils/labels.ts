import { UserRole } from '../models/auth.model';
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

/** Clases CSS del estado (definidas en styles.css). */
export const RESERVATION_STATUS_BADGES: Record<ReservationStatus, string> = {
  Pending: 'status-pill status-pending',
  Confirmed: 'status-pill status-confirmed',
  Completed: 'status-pill status-completed',
  Cancelled: 'status-pill status-cancelled',
  NoShow: 'status-pill status-danger',
};

export const TABLE_STATUS_LABELS: Record<TableStatus, string> = {
  Available: 'Disponible',
  Occupied: 'Ocupada',
  Reserved: 'Reservada',
  OutOfService: 'Fuera de servicio',
};

export const TABLE_STATUS_BADGES: Record<TableStatus, string> = {
  Available: 'status-pill status-confirmed',
  Occupied: 'status-pill status-danger',
  Reserved: 'status-pill status-completed',
  OutOfService: 'status-pill status-cancelled',
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

export const ROLE_LABELS: Record<UserRole, string> = {
  Admin: 'Administrador',
  Manager: 'Encargado',
  Staff: 'Camarero / sala',
};

export const ROLE_DESCRIPTIONS: Record<UserRole, string> = {
  Admin: 'Todo, incluido el personal y los datos del restaurante.',
  Manager: 'Reservas, clientes, mesas y carta. No gestiona el personal.',
  Staff: 'Reservas, clientes y estado de las mesas. No borra ni cambia la carta ni las mesas.',
};

export const ROLES = Object.keys(ROLE_LABELS) as UserRole[];

/** Fecha local en formato yyyy-MM-dd (para inputs type="date"). */
export function toDateInputValue(date: Date): string {
  const pad = (n: number) => n.toString().padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}
