import { Component, OnInit, ChangeDetectorRef } from '@angular/core'; // 1. Importamos ChangeDetectorRef
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { CustomerService } from '../../services/customer.service';
import { CustomerResponseDTO } from '../../models/customer.model';

@Component({
  selector: 'app-customer-list',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './customer-list.component.html'
})
export class CustomerListComponent implements OnInit {
  listaClientes: CustomerResponseDTO[] = [];

  // 2. Inyectamos cdr en el constructor
  constructor(
    private customerService: CustomerService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.cargarClientes();
  }

  cargarClientes(): void {
    this.customerService.getCustomers().subscribe({
      next: (datos) => {
        this.listaClientes = datos;
        // 3. Forzamos a Angular a renderizar los clientes de inmediato
        this.cdr.detectChanges(); 
      },
      error: (err) => console.error('Error al traer clientes:', err)
    });
  }

  onDelete(id: number): void {
    if (confirm('¿Estás completamente seguro de que deseas eliminar este cliente del sistema?')) {
      this.customerService.deleteCustomer(id).subscribe({
        next: () => {
          this.cargarClientes();
        },
        error: (err) => console.error('Error al eliminar cliente:', err)
      });
    }
  }
}