import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Observable } from 'rxjs';
import { StationResponseDTO, StationType } from '../../models/order.model';
import { StationService } from '../../services/station.service';
import { apiErrorMessage } from '../../utils/api-error';
import { STATION_TYPES, STATION_TYPE_LABELS } from '../../utils/labels';

/** Zonas de preparación: cada plato sale por la pantalla (y la impresora) de su zona. */
@Component({
  selector: 'app-station-list',
  standalone: true,
  imports: [ReactiveFormsModule],
  template: `
    <div class="page-header">
      <div>
        <h2>Zonas de preparación</h2>
        <p>Cocina, barra, parrilla... Cada plato de la carta va a una zona, que ve sus comandas en su pantalla.</p>
      </div>
      <button type="button" class="btn btn-primary px-4" (click)="openNew()">
        <i class="bi bi-plus-lg me-2"></i>Nueva zona
      </button>
    </div>

    @if (error()) {
      <div class="alert alert-danger d-flex justify-content-between align-items-center">
        <span><i class="bi bi-exclamation-triangle me-2"></i>{{ error() }}</span>
        <button type="button" class="btn-close" aria-label="Cerrar" (click)="error.set(null)"></button>
      </div>
    }

    @if (editingId() !== null) {
      <div class="card mb-4">
        <div class="card-body">
          <h5 class="fw-bold mb-3">{{ editingId() ? 'Editar zona' : 'Nueva zona' }}</h5>
          <form [formGroup]="form" (ngSubmit)="save()" class="row g-3 align-items-end">
            <div class="col-md-4">
              <label for="s-name" class="form-label small">Nombre</label>
              <input id="s-name" class="form-control" formControlName="name" maxlength="50" placeholder="Parrilla, Postres...">
            </div>
            <div class="col-6 col-md-2">
              <label for="s-type" class="form-label small">Tipo</label>
              <select id="s-type" class="form-select" formControlName="type">
                @for (t of types; track t) {
                  <option [ngValue]="t">{{ typeLabels[t] }}</option>
                }
              </select>
            </div>
            <div class="col-6 col-md-2">
              <label for="s-order" class="form-label small">Orden</label>
              <input id="s-order" type="number" min="0" max="999" class="form-control" formControlName="sortOrder">
            </div>
            <div class="col-md-4">
              <label for="s-printer" class="form-label small">Impresora de comandas <span class="text-muted">(opcional)</span></label>
              <input id="s-printer" class="form-control" formControlName="printerAddress" placeholder="192.168.1.50:9100"
                     [class.is-invalid]="form.controls.printerAddress.touched && form.controls.printerAddress.invalid">
              <div class="invalid-feedback">IP o nombre, con puerto opcional.</div>
            </div>
            <div class="col-12 d-flex justify-content-end gap-2">
              <button type="button" class="btn btn-outline-secondary" (click)="editingId.set(null)">Cancelar</button>
              <button type="submit" class="btn btn-primary" [disabled]="saving() || form.invalid">
                <i class="bi bi-save me-1"></i>Guardar
              </button>
            </div>
          </form>
        </div>
      </div>
    }

    <div class="card">
      <div class="table-responsive">
        <table class="table table-hover align-middle mb-0">
          <thead>
            <tr>
              <th class="ps-4">Zona</th>
              <th>Tipo</th>
              <th>Impresora</th>
              <th class="text-end pe-4">Acciones</th>
            </tr>
          </thead>
          <tbody>
            @for (s of stations(); track s.id) {
              <tr>
                <td class="ps-4 fw-semibold">{{ s.name }}</td>
                <td><span class="tag">{{ typeLabels[s.type] }}</span></td>
                <td>{{ s.printerAddress || '—' }}</td>
                <td class="text-end pe-4 text-nowrap">
                  <button type="button" class="btn btn-sm btn-outline-secondary me-1" title="Editar" (click)="openEdit(s)">
                    <i class="bi bi-pencil"></i>
                  </button>
                  <button type="button" class="btn btn-sm btn-state btn-state-cancel" title="Eliminar" (click)="remove(s)">
                    <i class="bi bi-trash"></i>
                  </button>
                </td>
              </tr>
            } @empty {
              <tr>
                <td colspan="4" class="text-center py-5 text-muted">
                  @if (loading()) {
                    <span class="spinner-border spinner-border-sm me-2"></span>Cargando...
                  } @else {
                    No hay zonas.
                  }
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    </div>
    <p class="text-muted small mt-3">
      Para cambiar la zona de un plato, edítalo en la carta. Una zona con platos no se puede borrar.
    </p>
  `,
})
export class StationListComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly stationService = inject(StationService);

  readonly types = STATION_TYPES;
  readonly typeLabels = STATION_TYPE_LABELS;

  readonly stations = signal<StationResponseDTO[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly editingId = signal<number | null>(null);

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(50)]],
    type: ['Kitchen' as StationType, Validators.required],
    sortOrder: [0, [Validators.min(0), Validators.max(999)]],
    printerAddress: ['', [Validators.maxLength(100), Validators.pattern(/^[A-Za-z0-9.-]+(:\d{1,5})?$/)]],
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.stationService.getAll().subscribe({
      next: (data) => {
        this.stations.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(apiErrorMessage(err, 'No se pudieron cargar las zonas.'));
      },
    });
  }

  openNew(): void {
    const next = Math.max(0, ...this.stations().map((s) => s.sortOrder)) + 1;
    this.form.reset({ name: '', type: 'Kitchen', sortOrder: next, printerAddress: '' });
    this.editingId.set(0);
  }

  openEdit(s: StationResponseDTO): void {
    this.form.reset({ name: s.name, type: s.type, sortOrder: s.sortOrder, printerAddress: s.printerAddress ?? '' });
    this.editingId.set(s.id);
  }

  save(): void {
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    const dto = { ...v, name: v.name.trim(), printerAddress: v.printerAddress.trim() || null };
    const id = this.editingId();
    this.saving.set(true);
    const request$: Observable<unknown> = id ? this.stationService.update(id, dto) : this.stationService.create(dto);
    request$.subscribe({
      next: () => {
        this.saving.set(false);
        this.editingId.set(null);
        this.load();
      },
      error: (err) => {
        this.saving.set(false);
        this.error.set(apiErrorMessage(err, 'No se pudo guardar la zona.'));
      },
    });
  }

  remove(s: StationResponseDTO): void {
    if (!confirm(`¿Eliminar la zona "${s.name}"?`)) return;
    this.stationService.delete(s.id).subscribe({
      next: () => this.load(),
      error: (err) => this.error.set(apiErrorMessage(err)),
    });
  }
}
