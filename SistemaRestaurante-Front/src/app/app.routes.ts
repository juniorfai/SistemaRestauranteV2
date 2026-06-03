import { Routes } from '@angular/router';
import { ReservationListComponent } from './components/reservation-list/reservation-list.component';
import { CustomerListComponent } from './components/customer-list/customer-list.component';


import { CustomerFormComponent } from './components/customer-form/customer-form';
import { MenuListComponent } from './components/menu-list/menu-list';

export const routes: Routes = [
  { path: '', redirectTo: 'mesas', pathMatch: 'full' },
  { path: 'mesas', component: ReservationListComponent },
  { path: 'clientes', component: CustomerListComponent },
  { path: 'clientes/nuevo', component: CustomerFormComponent },
  { path: 'clientes/editar/:id', component: CustomerFormComponent },
  
  { path: 'menu', component: MenuListComponent }, 
  
  { path: '**', redirectTo: 'mesas' }
];