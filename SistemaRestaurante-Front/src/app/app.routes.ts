import { Routes } from '@angular/router';
import { authGuard, guestGuard, roleGuard } from './auth/auth.guards';
import { AccountComponent } from './components/account/account';
import { LoginComponent } from './components/login/login';
import { UserListComponent } from './components/user-list/user-list';
import { MANAGEMENT_ROLES } from './services/auth.service';
import { CustomerFormComponent } from './components/customer-form/customer-form';
import { CustomerListComponent } from './components/customer-list/customer-list.component';
import { MenuFormComponent } from './components/menu-form/menu-form';
import { MenuListComponent } from './components/menu-list/menu-list';
import { ReservationFormComponent } from './components/reservation-form/reservation-form.component';
import { ReservationListComponent } from './components/reservation-list/reservation-list.component';
import { TableListComponent } from './components/table-list/table-list.component';

const management = { roles: MANAGEMENT_ROLES };

export const routes: Routes = [
  { path: 'login', component: LoginComponent, title: 'Iniciar sesión', canActivate: [guestGuard] },

  // Todo lo demás exige sesión.
  {
    path: '',
    canActivateChild: [authGuard],
    children: [
      { path: '', redirectTo: 'reservas', pathMatch: 'full' },

      { path: 'reservas', component: ReservationListComponent, title: 'Reservas' },
      { path: 'reservas/nueva', component: ReservationFormComponent, title: 'Nueva reserva' },
      { path: 'reservas/editar/:id', component: ReservationFormComponent, title: 'Editar reserva' },

      { path: 'mesas', component: TableListComponent, title: 'Mesas' },

      { path: 'clientes', component: CustomerListComponent, title: 'Clientes' },
      { path: 'clientes/nuevo', component: CustomerFormComponent, title: 'Nuevo cliente' },
      { path: 'clientes/editar/:id', component: CustomerFormComponent, title: 'Editar cliente' },

      { path: 'menu', component: MenuListComponent, title: 'Carta' },
      { path: 'menu/nuevo', component: MenuFormComponent, title: 'Nuevo plato', canActivate: [roleGuard], data: management },
      { path: 'menu/editar/:id', component: MenuFormComponent, title: 'Editar plato', canActivate: [roleGuard], data: management },

      { path: 'personal', component: UserListComponent, title: 'Personal', canActivate: [roleGuard], data: { roles: ['Admin'] } },
      { path: 'cuenta', component: AccountComponent, title: 'Mi cuenta' },
    ],
  },

  { path: '**', redirectTo: 'reservas' },
];
