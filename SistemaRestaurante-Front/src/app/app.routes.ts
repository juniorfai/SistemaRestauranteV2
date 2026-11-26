import { Routes } from '@angular/router';
import { CustomerFormComponent } from './components/customer-form/customer-form';
import { CustomerListComponent } from './components/customer-list/customer-list.component';
import { MenuFormComponent } from './components/menu-form/menu-form';
import { MenuListComponent } from './components/menu-list/menu-list';
import { ReservationFormComponent } from './components/reservation-form/reservation-form.component';
import { ReservationListComponent } from './components/reservation-list/reservation-list.component';
import { TableListComponent } from './components/table-list/table-list.component';

export const routes: Routes = [
  { path: '', redirectTo: 'reservas', pathMatch: 'full' },

  { path: 'reservas', component: ReservationListComponent, title: 'Reservas' },
  { path: 'reservas/nueva', component: ReservationFormComponent, title: 'Nueva reserva' },
  { path: 'reservas/editar/:id', component: ReservationFormComponent, title: 'Editar reserva' },

  { path: 'mesas', component: TableListComponent, title: 'Mesas' },

  { path: 'clientes', component: CustomerListComponent, title: 'Clientes' },
  { path: 'clientes/nuevo', component: CustomerFormComponent, title: 'Nuevo cliente' },
  { path: 'clientes/editar/:id', component: CustomerFormComponent, title: 'Editar cliente' },

  { path: 'menu', component: MenuListComponent, title: 'Carta' },
  { path: 'menu/nuevo', component: MenuFormComponent, title: 'Nuevo plato' },
  { path: 'menu/editar/:id', component: MenuFormComponent, title: 'Editar plato' },

  { path: '**', redirectTo: 'reservas' },
];
