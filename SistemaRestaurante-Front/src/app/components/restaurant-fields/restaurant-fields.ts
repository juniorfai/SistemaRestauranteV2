import { Component, input } from '@angular/core';
import { FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RestaurantRequestDTO, RestaurantResponseDTO } from '../../models/restaurant.model';

export type RestaurantForm = FormGroup<{
  name: FormControl<string>;
  address: FormControl<string>;
  phoneNumber: FormControl<string>;
  email: FormControl<string>;
  openingHours: FormControl<string>;
}>;

/** Mismas reglas que la API (RestaurantRequestDTO). */
export function buildRestaurantForm(fb: FormBuilder): RestaurantForm {
  return fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    address: ['', [Validators.required, Validators.maxLength(200)]],
    phoneNumber: ['', [Validators.required, Validators.pattern(/^\+?[0-9\s-]{7,20}$/)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(150)]],
    openingHours: ['', Validators.maxLength(200)],
  });
}

export function restaurantFormValue(restaurant?: RestaurantResponseDTO | null) {
  return {
    name: restaurant?.name ?? '',
    address: restaurant?.address ?? '',
    phoneNumber: restaurant?.phoneNumber ?? '',
    email: restaurant?.email ?? '',
    openingHours: restaurant?.openingHours ?? '',
  };
}

export function toRestaurantRequest(form: RestaurantForm): RestaurantRequestDTO {
  const v = form.getRawValue();
  return {
    name: v.name.trim(),
    address: v.address.trim(),
    phoneNumber: v.phoneNumber.trim(),
    email: v.email.trim(),
    openingHours: v.openingHours.trim() || null,
  };
}

/** Campos con los datos de un restaurante, para el alta y la edición. */
@Component({
  selector: 'app-restaurant-fields',
  standalone: true,
  imports: [ReactiveFormsModule],
  template: `
    <div class="row g-3" [formGroup]="group()">
      <div class="col-md-6">
        <label for="r-name" class="form-label">Nombre del restaurante</label>
        <input id="r-name" class="form-control" formControlName="name" autocomplete="off"
               [class.is-invalid]="invalid('name')">
        <div class="invalid-feedback">Escribe el nombre (máximo 100 caracteres).</div>
      </div>
      <div class="col-md-6">
        <label for="r-address" class="form-label">Dirección</label>
        <input id="r-address" class="form-control" formControlName="address" autocomplete="off"
               [class.is-invalid]="invalid('address')">
        <div class="invalid-feedback">La dirección es obligatoria.</div>
      </div>
      <div class="col-md-6">
        <label for="r-phone" class="form-label">Teléfono</label>
        <input id="r-phone" type="tel" class="form-control" formControlName="phoneNumber" autocomplete="off"
               [class.is-invalid]="invalid('phoneNumber')">
        <div class="invalid-feedback">De 7 a 20 dígitos; se admiten espacios, guiones y + inicial.</div>
      </div>
      <div class="col-md-6">
        <label for="r-email" class="form-label">Email</label>
        <input id="r-email" type="email" class="form-control" formControlName="email" autocomplete="off"
               [class.is-invalid]="invalid('email')">
        <div class="invalid-feedback">Escribe un email válido.</div>
      </div>
      <div class="col-12">
        <label for="r-hours" class="form-label">Horario <span class="text-muted">(opcional)</span></label>
        <input id="r-hours" class="form-control" formControlName="openingHours" autocomplete="off"
               placeholder="Ej.: L-D 13:00-16:00 y 20:00-23:30" [class.is-invalid]="invalid('openingHours')">
        <div class="invalid-feedback">Máximo 200 caracteres.</div>
      </div>
    </div>
  `,
})
export class RestaurantFieldsComponent {
  readonly group = input.required<RestaurantForm>();

  invalid(name: keyof RestaurantForm['controls']): boolean {
    const control = this.group().controls[name];
    return control.touched && control.invalid;
  }
}
