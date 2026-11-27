import { RestaurantRole, UserRole } from '../models/auth.model';
import { Allergen, MenuItemCategory } from '../models/menu.model';
import { OrderCourse, OrderItemStatus, PaymentMethod, StationType } from '../models/order.model';
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
  Kitchen: 'Cocina',
  PlatformAdmin: 'Plataforma',
};

export const ROLE_DESCRIPTIONS: Record<RestaurantRole, string> = {
  Admin: 'Todo, incluido el personal y los datos del restaurante.',
  Manager: 'Sala y comandas (también anularlas), reservas, clientes, mesas, carta y zonas. No gestiona el personal.',
  Staff: 'Sala y comandas, reservas, clientes y estado de las mesas. No anula lo enviado a cocina ni cambia la carta.',
  Kitchen: 'Solo la pantalla de cocina y barra (para la tableta o pantalla de la cocina).',
};

/** Roles que se pueden asignar al personal de un restaurante. */
export const ROLES: RestaurantRole[] = ['Admin', 'Manager', 'Staff', 'Kitchen'];

/** Fecha local en formato yyyy-MM-dd (para inputs type="date"). */
export function toDateInputValue(date: Date): string {
  const pad = (n: number) => n.toString().padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}

// ---------- Comandas ----------

export const ALLERGEN_LABELS: Record<Allergen, string> = {
  Gluten: 'Gluten',
  Crustaceans: 'Crustáceos',
  Eggs: 'Huevo',
  Fish: 'Pescado',
  Peanuts: 'Maní',
  Soy: 'Soya',
  Milk: 'Lácteos',
  TreeNuts: 'Frutos secos',
  Celery: 'Apio',
  Mustard: 'Mostaza',
  Sesame: 'Ajonjolí',
  Sulphites: 'Sulfitos',
  Lupin: 'Altramuz',
  Molluscs: 'Moluscos',
};

export const ALLERGENS = Object.keys(ALLERGEN_LABELS) as Allergen[];

export const COURSE_LABELS: Record<OrderCourse, string> = {
  Immediate: 'Al momento',
  Starter: 'Entrada',
  Main: 'Plato fuerte',
  Dessert: 'Postre',
};

export const COURSES = Object.keys(COURSE_LABELS) as OrderCourse[];

/** Tiempo en que sale un plato si el camarero no dice otro (igual que la API). */
export const DEFAULT_COURSE: Record<MenuItemCategory, OrderCourse> = {
  Appetizer: 'Starter',
  MainCourse: 'Main',
  SideDish: 'Main',
  Dessert: 'Dessert',
  Drink: 'Immediate',
};

export const ITEM_STATUS_LABELS: Record<OrderItemStatus, string> = {
  Draft: 'Sin enviar',
  Held: 'Retenido',
  Sent: 'En cocina',
  Preparing: 'Preparando',
  Ready: 'Listo',
  Served: 'Servido',
  Cancelled: 'Anulado',
};

export const ITEM_STATUS_BADGES: Record<OrderItemStatus, string> = {
  Draft: 'status-pill status-pending',
  Held: 'status-pill status-cancelled',
  Sent: 'status-pill status-completed',
  Preparing: 'status-pill status-completed',
  Ready: 'status-pill status-confirmed',
  Served: 'status-pill status-cancelled',
  Cancelled: 'status-pill status-danger',
};

export const STATION_TYPE_LABELS: Record<StationType, string> = {
  Kitchen: 'Cocina',
  Bar: 'Barra',
  Other: 'Otra',
};

export const STATION_TYPES = Object.keys(STATION_TYPE_LABELS) as StationType[];

export const PAYMENT_METHOD_LABELS: Record<PaymentMethod, string> = {
  Cash: 'Efectivo',
  Card: 'Tarjeta',
  Transfer: 'Transferencia',
  Other: 'Otro',
};

export const PAYMENT_METHOD_ICONS: Record<PaymentMethod, string> = {
  Cash: 'bi-cash-stack',
  Card: 'bi-credit-card',
  Transfer: 'bi-phone',
  Other: 'bi-three-dots',
};

export const PAYMENT_METHODS = Object.keys(PAYMENT_METHOD_LABELS) as PaymentMethod[];
