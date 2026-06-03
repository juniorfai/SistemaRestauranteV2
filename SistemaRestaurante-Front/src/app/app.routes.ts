import { Routes } from '@angular/router';
import { ReservationListComponent } from './components/reservation-list/reservation-list.component';
import { CustomerListComponent } from './components/customer-list/customer-list.component';
import { CustomerFormComponent } from './components/customer-form/customer-form';


export const routes: Routes = [
  { path: '', redirectTo: 'mesas', pathMatch: 'full' },
  { path: 'mesas', component: ReservationListComponent },
  
  // Rutas del Módulo de Clientes
  { path: 'clientes', component: CustomerListComponent },
  { path: 'clientes/nuevo', component: CustomerFormComponent }, // Ruta para crear
  { path: 'clientes/editar/:id', component: CustomerFormComponent }, // Ruta para editar pasando el ID
  
  { path: '**', redirectTo: 'mesas' }
];