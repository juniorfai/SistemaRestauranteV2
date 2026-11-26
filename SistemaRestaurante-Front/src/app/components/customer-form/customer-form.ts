import { Component, OnInit, inject, signal } from '@angular/core';
import { NgClass } from '@angular/common';
import { Observable } from 'rxjs';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CustomerRequestDTO } from '../../models/customer.model';
import { CustomerService } from '../../services/customer.service';
import { apiErrorMessage } from '../../utils/api-error';

@Component({
  selector: 'app-customer-form',
  standalone: true,
  imports: [NgClass, ReactiveFormsModule, RouterLink],
  templateUrl: './customer-form.html',
})
export class CustomerFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly customerService = inject(CustomerService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly customerForm = this.fb.nonNullable.group({
    firstName: ['', [Validators.required, Validators.maxLength(50)]],
    lastName: ['', [Validators.required, Validators.maxLength(50)]],
    email: ['', [Validators.required, Validators.email]],
    phoneNumber: ['', [Validators.required, Validators.pattern('^\\+?[0-9\\s-]{7,20}$')]],
    preferences: ['', Validators.maxLength(500)],
  });

  isEditMode = false;
  private customerId = 0;

  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.isEditMode = true;
      this.customerId = Number(idParam);
      this.customerService.getCustomerById(this.customerId).subscribe({
        next: (customer) =>
          this.customerForm.patchValue({
            firstName: customer.firstName,
            lastName: customer.lastName,
            email: customer.email,
            phoneNumber: customer.phoneNumber,
            preferences: customer.preferences ?? '',
          }),
        error: (err) => this.error.set(apiErrorMessage(err, 'No se pudo cargar el cliente.')),
      });
    }
  }

  onSubmit(): void {
    if (this.customerForm.invalid) {
      this.customerForm.markAllAsTouched();
      return;
    }

    const value = this.customerForm.getRawValue();
    const request: CustomerRequestDTO = { ...value, preferences: value.preferences || null };

    this.saving.set(true);
    this.error.set(null);

    const request$: Observable<unknown> = this.isEditMode
      ? this.customerService.updateCustomer(this.customerId, request)
      : this.customerService.createCustomer(request);

    request$.subscribe({
      next: () => this.router.navigate(['/clientes']),
      error: (err) => {
        this.error.set(apiErrorMessage(err, 'No se pudo guardar el cliente.'));
        this.saving.set(false);
      },
    });
  }
}
