import { Component, OnInit, inject, signal } from '@angular/core';
import { Observable } from 'rxjs';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RestaurantResponseDTO } from '../../models/restaurant.model';
import { TableRequestDTO, TableResponseDTO, TableStatus } from '../../models/table.model';
import { RestaurantService } from '../../services/restaurant.service';
import { TableService } from '../../services/table.service';
import { apiErrorMessage } from '../../utils/api-error';
import { TABLE_STATUS_BADGES, TABLE_STATUS_LABELS, TABLE_STATUSES } from '../../utils/labels';

@Component({
  selector: 'app-table-list',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './table-list.component.html',
})
export class TableListComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly tableService = inject(TableService);
  private readonly restaurantService = inject(RestaurantService);

  readonly statuses = TABLE_STATUSES;
  readonly statusLabels = TABLE_STATUS_LABELS;
  readonly statusBadges = TABLE_STATUS_BADGES;

  readonly tables = signal<TableResponseDTO[]>([]);
  readonly restaurants = signal<RestaurantResponseDTO[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  /** null = formulario cerrado; 0 = mesa nueva; >0 = editando esa mesa. */
  readonly editingId = signal<number | null>(null);
  readonly saving = signal(false);

  readonly form = this.fb.nonNullable.group({
    tableNumber: [1, [Validators.required, Validators.min(1)]],
    capacity: [4, [Validators.required, Validators.min(1), Validators.max(50)]],
    location: ['', Validators.maxLength(100)],
    status: ['Available' as TableStatus, Validators.required],
    restaurantId: [0, [Validators.required, Validators.min(1)]],
  });

  ngOnInit(): void {
    this.load();
    this.restaurantService.getRestaurants().subscribe({
      next: (data) => this.restaurants.set(data),
    });
  }

  load(): void {
    this.loading.set(true);
    this.tableService.getTables().subscribe({
      next: (data) => {
        this.tables.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(apiErrorMessage(err, 'No se pudieron cargar las mesas.'));
        this.loading.set(false);
      },
    });
  }

  openNew(): void {
    const nextNumber = Math.max(0, ...this.tables().map((t) => t.tableNumber)) + 1;
    this.form.reset({
      tableNumber: nextNumber,
      capacity: 4,
      location: '',
      status: 'Available',
      restaurantId: this.restaurants()[0]?.id ?? 0,
    });
    this.editingId.set(0);
  }

  openEdit(table: TableResponseDTO): void {
    this.form.reset({
      tableNumber: table.tableNumber,
      capacity: table.capacity,
      location: table.location ?? '',
      status: table.status,
      restaurantId: table.restaurantId,
    });
    this.editingId.set(table.id);
  }

  closeForm(): void {
    this.editingId.set(null);
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const request: TableRequestDTO = { ...value, location: value.location || null };
    const id = this.editingId();

    this.saving.set(true);
    this.error.set(null);

    const request$: Observable<unknown> = id ? this.tableService.updateTable(id, request) : this.tableService.createTable(request);
    request$.subscribe({
      next: () => {
        this.saving.set(false);
        this.closeForm();
        this.load();
      },
      error: (err) => {
        this.error.set(apiErrorMessage(err, 'No se pudo guardar la mesa.'));
        this.saving.set(false);
      },
    });
  }

  changeStatus(table: TableResponseDTO, status: TableStatus): void {
    if (status === table.status) {
      return;
    }
    this.tableService.updateStatus(table.id, status).subscribe({
      next: () => this.load(),
      error: (err) => this.error.set(apiErrorMessage(err)),
    });
  }

  remove(table: TableResponseDTO): void {
    if (!confirm(`¿Eliminar la mesa ${table.tableNumber}?`)) {
      return;
    }
    this.tableService.deleteTable(table.id).subscribe({
      next: () => this.load(),
      error: (err) => this.error.set(apiErrorMessage(err)),
    });
  }
}
