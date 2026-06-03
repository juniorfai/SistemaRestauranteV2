import { Component, OnInit } from '@angular/core';
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

  constructor(private customerService: CustomerService) {}

  ngOnInit(): void {
    this.cargarClientes();
  }

  cargarClientes(): void {
    this.customerService.getCustomers().subscribe({
      next: (datos) => this.listaClientes = datos,
      error: (err) => console.error('Error al traer clientes:', err)
    });
  }

  onDelete(id: number): void {
    if (confirm('¿Estás completamente seguro de que deseas eliminar este cliente del sistema?')) {
      this.customerService.deleteCustomer(id).subscribe({
        next: () => {
          // Refresca la lista automáticamente de la base de datos tras borrar
          this.cargarClientes();
        },
        error: (err) => console.error('Error al eliminar cliente:', err)
      });
    }
  }
}