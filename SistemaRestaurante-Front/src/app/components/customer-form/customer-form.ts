import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { CustomerService } from '../../services/customer.service';
import { CustomerRequestDTO } from '../../models/customer.model';

@Component({
  selector: 'app-customer-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './customer-form.html'
})
export class CustomerFormComponent implements OnInit {
  customerForm!: FormGroup;
  isEditMode = false;
  customerId!: number;

  constructor(
    private fb: FormBuilder,
    private customerService: CustomerService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.initForm();

    // Detectar si venimos a editar mediante el ID en la URL
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.isEditMode = true;
      this.customerId = parseInt(idParam, 10);
      this.loadCustomerData();
    }
  }

  private initForm(): void {
    this.customerForm = this.fb.group({
      firstName: ['', [Validators.required, Validators.maxLength(50)]],
      lastName: ['', [Validators.required, Validators.maxLength(50)]],
      email: ['', [Validators.required, Validators.email]],
      phoneNumber: ['', [Validators.required, Validators.pattern('^[0-9\\s-]{7,15}$')]],
      preferences: ['']
    });
  }

  private loadCustomerData(): void {
    this.customerService.getCustomerById(this.customerId).subscribe({
      next: (customer) => {
        // Poblamos el formulario con las respuestas de la BD tolerando mayúsculas/minúsculas
        this.customerForm.patchValue({
          firstName: customer.firstName || customer['firstName'],
          lastName: customer.lastName || customer['lastName'],
          email: customer.email || customer['email'],
          phoneNumber: customer.phoneNumber || customer['phoneNumber'],
          preferences: customer.preferences || customer['preferences']
        });
      },
      error: (err) => console.error('Error al cargar cliente:', err)
    });
  }

  onSubmit(): void {
    if (this.customerForm.invalid) {
      this.customerForm.markAllAsTouched();
      return;
    }

    const requestData: CustomerRequestDTO = this.customerForm.value;

    if (this.isEditMode) {
      this.customerService.updateCustomer(this.customerId, requestData).subscribe({
        next: () => this.router.navigate(['/clientes']),
        error: (err) => console.error('Error al actualizar:', err)
      });
    } else {
      this.customerService.createCustomer(requestData).subscribe({
        next: () => this.router.navigate(['/clientes']),
        error: (err) => console.error('Error al registrar:', err)
      });
    }
  }
}