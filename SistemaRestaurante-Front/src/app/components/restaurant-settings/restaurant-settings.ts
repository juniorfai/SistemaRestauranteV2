import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RestaurantResponseDTO } from '../../models/restaurant.model';
import { AuthService } from '../../services/auth.service';
import { RestaurantService } from '../../services/restaurant.service';
import { apiErrorMessage } from '../../utils/api-error';
import {
  RestaurantFieldsComponent,
  buildRestaurantForm,
  restaurantFormValue,
  toRestaurantRequest,
} from '../restaurant-fields/restaurant-fields';

/** Datos del propio restaurante. Solo su administrador. */
@Component({
  selector: 'app-restaurant-settings',
  standalone: true,
  imports: [ReactiveFormsModule, RestaurantFieldsComponent],
  template: `
    <div class="page-header">
      <div>
        <h2>Mi restaurante</h2>
        <p>Datos de contacto y horario del restaurante.</p>
      </div>
    </div>

    @if (notice()) {
      <div class="alert alert-success-soft d-flex justify-content-between align-items-center">
        <span><i class="bi bi-check-circle me-2"></i>{{ notice() }}</span>
        <button type="button" class="btn-close" aria-label="Cerrar" (click)="notice.set(null)"></button>
      </div>
    }
    @if (error()) {
      <div class="alert alert-danger"><i class="bi bi-exclamation-triangle me-2"></i>{{ error() }}</div>
    }

    <div class="card" style="max-width: 820px;">
      <div class="card-body p-4">
        @if (restaurant()) {
          <form [formGroup]="form" (ngSubmit)="save()" novalidate>
            <app-restaurant-fields [group]="form" />
            <div class="d-flex justify-content-end mt-4">
              <button type="submit" class="btn btn-primary px-4" [disabled]="saving()">
                @if (saving()) { <span class="spinner-border spinner-border-sm me-2"></span> }
                Guardar cambios
              </button>
            </div>
          </form>
        } @else if (!error()) {
          <div class="empty-state"><span class="spinner-border spinner-border-sm me-2"></span>Cargando...</div>
        }
      </div>
    </div>
  `,
})
export class RestaurantSettingsComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly restaurantService = inject(RestaurantService);
  private readonly auth = inject(AuthService);

  readonly form = buildRestaurantForm(this.fb);
  readonly restaurant = signal<RestaurantResponseDTO | null>(null);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly notice = signal<string | null>(null);

  ngOnInit(): void {
    this.restaurantService.getCurrent().subscribe({
      next: (data) => {
        this.restaurant.set(data);
        this.form.reset(restaurantFormValue(data));
      },
      error: (err) => this.error.set(apiErrorMessage(err, 'No se pudo cargar el restaurante.')),
    });
  }

  save(): void {
    const current = this.restaurant();
    if (!current) return;
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const request = toRestaurantRequest(this.form);
    this.saving.set(true);
    this.error.set(null);
    this.notice.set(null);
    this.restaurantService.update(current.id, request).subscribe({
      next: () => {
        this.saving.set(false);
        this.restaurant.set({ ...current, ...request });
        this.auth.setRestaurantName(request.name);
        this.notice.set('Datos del restaurante guardados.');
      },
      error: (err) => {
        this.saving.set(false);
        this.error.set(apiErrorMessage(err, 'No se pudieron guardar los cambios.'));
      },
    });
  }
}
