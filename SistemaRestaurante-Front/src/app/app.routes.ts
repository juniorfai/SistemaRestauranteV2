import { Routes } from '@angular/router';
import { authGuard, guestGuard, homeRedirect, roleGuard } from './auth/auth.guards';
import { AccountComponent } from './components/account/account';
import { LoginComponent } from './components/login/login';
import { UserListComponent } from './components/user-list/user-list';
import { KITCHEN_SCREEN_ROLES, MANAGEMENT_ROLES, RESTAURANT_ROLES } from './services/auth.service';
import { RestaurantListComponent } from './components/restaurant-list/restaurant-list';
import { RestaurantSettingsComponent } from './components/restaurant-settings/restaurant-settings';
import { CustomerFormComponent } from './components/customer-form/customer-form';
import { CustomerListComponent } from './components/customer-list/customer-list.component';
import { MenuFormComponent } from './components/menu-form/menu-form';
import { MenuListComponent } from './components/menu-list/menu-list';
import { ReservationFormComponent } from './components/reservation-form/reservation-form.component';
import { ReservationListComponent } from './components/reservation-list/reservation-list.component';
import { TableListComponent } from './components/table-list/table-list.component';
import { FloorComponent } from './components/floor/floor';
import { OrderDetailComponent } from './components/order-detail/order-detail';
import { StationListComponent } from './components/station-list/station-list';

const management = { roles: MANAGEMENT_ROLES };
const staff = { roles: RESTAURANT_ROLES };

export const routes: Routes = [
  { path: 'login', component: LoginComponent, title: 'Iniciar sesión', canActivate: [guestGuard] },

  // Todo lo demás exige sesión.
  {
    path: '',
    canActivateChild: [authGuard],
    children: [
      { path: '', redirectTo: homeRedirect, pathMatch: 'full' },

      // Administración de la plataforma
      { path: 'restaurantes', component: RestaurantListComponent, title: 'Restaurantes', canActivate: [roleGuard], data: { roles: ['PlatformAdmin'] } },

      { path: 'sala', component: FloorComponent, title: 'Sala', canActivate: [roleGuard], data: staff },
      { path: 'sala/cuenta/:id', component: OrderDetailComponent, title: 'Cuenta', canActivate: [roleGuard], data: staff },
      { path: 'sala/cuenta/:id/cobrar', loadComponent: () => import('./components/bill/bill').then((m) => m.BillComponent), title: 'Cobrar', canActivate: [roleGuard], data: staff },

      { path: 'cocina', loadComponent: () => import('./components/kitchen/kitchen').then((m) => m.KitchenComponent), title: 'Cocina', canActivate: [roleGuard], data: { roles: KITCHEN_SCREEN_ROLES } },

      { path: 'reservas', component: ReservationListComponent, title: 'Reservas', canActivate: [roleGuard], data: staff },
      { path: 'reservas/nueva', component: ReservationFormComponent, title: 'Nueva reserva', canActivate: [roleGuard], data: staff },
      { path: 'reservas/editar/:id', component: ReservationFormComponent, title: 'Editar reserva', canActivate: [roleGuard], data: staff },

      { path: 'mesas', component: TableListComponent, title: 'Mesas', canActivate: [roleGuard], data: staff },

      { path: 'clientes', component: CustomerListComponent, title: 'Clientes', canActivate: [roleGuard], data: staff },
      { path: 'clientes/nuevo', component: CustomerFormComponent, title: 'Nuevo cliente', canActivate: [roleGuard], data: staff },
      { path: 'clientes/editar/:id', component: CustomerFormComponent, title: 'Editar cliente', canActivate: [roleGuard], data: staff },

      { path: 'menu', component: MenuListComponent, title: 'Carta', canActivate: [roleGuard], data: staff },
      { path: 'menu/nuevo', component: MenuFormComponent, title: 'Nuevo plato', canActivate: [roleGuard], data: management },
      { path: 'menu/editar/:id', component: MenuFormComponent, title: 'Editar plato', canActivate: [roleGuard], data: management },

      { path: 'caja', loadComponent: () => import('./components/cash-report/cash-report').then((m) => m.CashReportComponent), title: 'Caja del día', canActivate: [roleGuard], data: management },

      { path: 'zonas', component: StationListComponent, title: 'Zonas de preparación', canActivate: [roleGuard], data: management },

      { path: 'personal', component: UserListComponent, title: 'Personal', canActivate: [roleGuard], data: { roles: ['Admin', 'PlatformAdmin'] } },
      { path: 'restaurante', component: RestaurantSettingsComponent, title: 'Mi restaurante', canActivate: [roleGuard], data: { roles: ['Admin'] } },
      { path: 'cuenta', component: AccountComponent, title: 'Mi cuenta' },
    ],
  },

  { path: '**', redirectTo: homeRedirect },
];
