import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CustomerResponseDTO } from '../../models/customer.model';
import { CustomerService } from '../../services/customer.service';
import { apiErrorMessage } from '../../utils/api-error';

@Component({
  selector: 'app-customer-list',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './customer-list.component.html',
})
export class CustomerListComponent implements OnInit {
  private readonly customerService = inject(CustomerService);

  readonly customers = signal<CustomerResponseDTO[]>([]);
  readonly search = signal('');
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  readonly filteredCustomers = computed(() => {
    const term = this.search().trim().toLowerCase();
    if (!term) {
      return this.customers();
    }
    return this.customers().filter((c) =>
      `${c.firstName} ${c.lastName} ${c.email} ${c.phoneNumber}`.toLowerCase().includes(term),
    );
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.customerService.getCustomers().subscribe({
      next: (data) => {
        this.customers.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(apiErrorMessage(err, 'No se pudieron cargar los clientes.'));
        this.loading.set(false);
      },
    });
  }

  onDelete(customer: CustomerResponseDTO): void {
    if (!confirm(`¿Eliminar a ${customer.firstName} ${customer.lastName}?`)) {
      return;
    }
    this.customerService.deleteCustomer(customer.id).subscribe({
      next: () => this.load(),
      error: (err) => this.error.set(apiErrorMessage(err)),
    });
  }
}
