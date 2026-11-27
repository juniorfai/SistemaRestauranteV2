import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Observable } from 'rxjs';
import { RestaurantResponseDTO } from '../../models/restaurant.model';
import { RestaurantService } from '../../services/restaurant.service';
import { apiErrorMessage } from '../../utils/api-error';
import {
  RestaurantFieldsComponent,
  buildRestaurantForm,
  restaurantFormValue,
  toRestaurantRequest,
} from '../restaurant-fields/restaurant-fields';

type Panel = { mode: 'closed' } | { mode: 'new' } | { mode: 'edit'; restaurant: RestaurantResponseDTO };

/**
 * Restaurantes clientes del programa: alta (con su primer administrador), edición y
 * suspensión. Solo el administrador de la plataforma.
 */
@Component({
  selector: 'app-restaurant-list',
  standalone: true,
  imports: [ReactiveFormsModule, DatePipe, RestaurantFieldsComponent],
  templateUrl: './restaurant-list.html',
})
export class RestaurantListComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly restaurantService = inject(RestaurantService);

  readonly restaurants = signal<RestaurantResponseDTO[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly notice = signal<string | null>(null);
  readonly panel = signal<Panel>({ mode: 'closed' });

  readonly activeCount = computed(() => this.restaurants().filter((r) => r.isActive).length);

  readonly form = buildRestaurantForm(this.fb);
  readonly adminForm = this.fb.nonNullable.group({
    adminFullName: ['', [Validators.required, Validators.maxLength(100)]],
    adminUsername: ['', [Validators.required, Validators.pattern(/^[a-zA-Z0-9._-]{3,50}$/)]],
    adminPassword: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(100)]],
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.restaurantService.getRestaurants().subscribe({
      next: (data) => {
        this.restaurants.set([...data].sort((a, b) => a.name.localeCompare(b.name, 'es')));
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(apiErrorMessage(err, 'No se pudieron cargar los restaurantes.'));
        this.loading.set(false);
      },
    });
  }

  /** La API guarda las fechas en UTC sin indicar la zona; se marca para mostrarlas en hora local. */
  asUtc(value: string): string {
    return /Z|[+-]\d\d:\d\d$/.test(value) ? value : `${value}Z`;
  }

  openNew(): void {
    this.form.reset(restaurantFormValue());
    this.adminForm.reset({ adminFullName: '', adminUsername: '', adminPassword: '' });
    this.open({ mode: 'new' });
  }

  openEdit(restaurant: RestaurantResponseDTO): void {
    this.form.reset(restaurantFormValue(restaurant));
    this.open({ mode: 'edit', restaurant });
  }

  close(): void {
    this.panel.set({ mode: 'closed' });
  }

  save(): void {
    const panel = this.panel();
    const adminNeeded = panel.mode === 'new';
    if (this.form.invalid || (adminNeeded && this.adminForm.invalid)) {
      this.form.markAllAsTouched();
      this.adminForm.markAllAsTouched();
      return;
    }

    const request = toRestaurantRequest(this.form);
    let request$: Observable<unknown>;
    let done: string;
    if (panel.mode === 'new') {
      const admin = this.adminForm.getRawValue();
      request$ = this.restaurantService.create({
        ...request,
        adminFullName: admin.adminFullName.trim(),
        adminUsername: admin.adminUsername.trim(),
        adminPassword: admin.adminPassword,
      });
      done = `Restaurante «${request.name}» creado. Su administrador ya puede entrar como ${admin.adminUsername.trim().toLowerCase()}.`;
    } else if (panel.mode === 'edit') {
      request$ = this.restaurantService.update(panel.restaurant.id, request);
      done = 'Cambios guardados.';
    } else {
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    request$.subscribe({
      next: () => {
        this.saving.set(false);
        this.notice.set(done);
        this.close();
        this.load();
      },
      error: (err) => {
        this.saving.set(false);
        this.error.set(apiErrorMessage(err, 'No se pudieron guardar los cambios.'));
      },
    });
  }

  toggleActive(restaurant: RestaurantResponseDTO): void {
    const suspend = restaurant.isActive;
    if (
      suspend &&
      !confirm(`¿Suspender «${restaurant.name}»? Su personal no podrá entrar y se cerrarán las sesiones abiertas. Los datos no se borran.`)
    ) {
      return;
    }

    this.error.set(null);
    this.notice.set(null);
    this.restaurantService.setActive(restaurant.id, !suspend).subscribe({
      next: () => {
        this.notice.set(suspend ? `«${restaurant.name}» suspendido.` : `«${restaurant.name}» reactivado.`);
        this.load();
      },
      error: (err) => this.error.set(apiErrorMessage(err)),
    });
  }

  private open(panel: Panel): void {
    this.error.set(null);
    this.notice.set(null);
    this.panel.set(panel);
  }
}
