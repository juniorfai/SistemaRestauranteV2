import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { MoneyPipe } from '../../services/money.service';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CustomerResponseDTO } from '../../models/customer.model';
import { MenuItemResponseDTO } from '../../models/menu.model';
import { ReservationRequestDTO } from '../../models/reservation.model';
import { TableResponseDTO } from '../../models/table.model';
import { CustomerService } from '../../services/customer.service';
import { MenuService } from '../../services/menu.service';
import { ReservationService } from '../../services/reservation.service';
import { TableService } from '../../services/table.service';
import { apiErrorMessage } from '../../utils/api-error';
import { MENU_CATEGORY_LABELS } from '../../utils/labels';

@Component({
  selector: 'app-reservation-form',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, MoneyPipe],
  templateUrl: './reservation-form.component.html',
})
export class ReservationFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly reservationService = inject(ReservationService);
  private readonly customerService = inject(CustomerService);
  private readonly tableService = inject(TableService);
  private readonly menuService = inject(MenuService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly categoryLabels = MENU_CATEGORY_LABELS;

  readonly form = this.fb.nonNullable.group({
    customerId: [0, [Validators.required, Validators.min(1)]],
    reservationDateTime: ['', Validators.required],
    numberOfPeople: [2, [Validators.required, Validators.min(1), Validators.max(50)]],
    tableId: [0, [Validators.required, Validators.min(1)]],
    specialRequests: ['', Validators.maxLength(500)],
  });

  readonly reservationId = signal<number | null>(null);
  readonly isEditMode = computed(() => this.reservationId() !== null);

  readonly customers = signal<CustomerResponseDTO[]>([]);
  readonly availableTables = signal<TableResponseDTO[]>([]);
  readonly tablesSearched = signal(false);
  readonly menuItems = signal<MenuItemResponseDTO[]>([]);
  /** Cantidad pedida por adelantado de cada plato (id → cantidad). Solo al crear. */
  readonly quantities = signal<Record<number, number>>({});

  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  /** Mesa que la reserva ya tenía al entrar a editar (no aparece como "libre" porque la ocupa ella misma). */
  private currentTable: TableResponseDTO | null = null;

  readonly preorderTotal = computed(() => {
    const qty = this.quantities();
    return this.menuItems().reduce((sum, item) => sum + item.price * (qty[item.id] ?? 0), 0);
  });

  ngOnInit(): void {
    this.customerService.getCustomers().subscribe({
      next: (data) => this.customers.set(data),
      error: (err) => this.error.set(apiErrorMessage(err, 'No se pudieron cargar los clientes.')),
    });

    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.reservationId.set(Number(idParam));
      this.loadReservation(Number(idParam));
    } else {
      this.menuService.getMenuItems().subscribe({
        next: (data) => this.menuItems.set(data),
      });
    }
  }

  private loadReservation(id: number): void {
    this.reservationService.getById(id).subscribe({
      next: (r) => {
        this.form.patchValue({
          customerId: r.customerId,
          reservationDateTime: r.reservationDateTime.substring(0, 16),
          numberOfPeople: r.numberOfPeople,
          tableId: r.tableId,
          specialRequests: r.specialRequests ?? '',
        });
        this.form.controls.customerId.disable();
        this.currentTable = {
          id: r.tableId,
          tableNumber: r.tableNumber,
          capacity: r.tableCapacity,
          status: 'Reserved',
          restaurantId: 0,
          restaurantName: '',
          isAvailable: false,
        };
        this.searchTables();
      },
      error: (err) => this.error.set(apiErrorMessage(err, 'No se pudo cargar la reserva.')),
    });
  }

  /** Busca las mesas libres para la fecha y el número de personas elegidos. */
  searchTables(): void {
    const { reservationDateTime, numberOfPeople } = this.form.getRawValue();
    if (!reservationDateTime || !numberOfPeople || numberOfPeople < 1) {
      this.availableTables.set([]);
      this.tablesSearched.set(false);
      return;
    }

    this.tableService.getAvailable(reservationDateTime, numberOfPeople).subscribe({
      next: (tables) => {
        const current = this.currentTable;
        if (current && current.capacity >= numberOfPeople && !tables.some((t) => t.id === current.id)) {
          tables = [current, ...tables];
        }
        this.availableTables.set(tables);
        this.tablesSearched.set(true);

        const selected = this.form.controls.tableId.value;
        if (!tables.some((t) => t.id === selected)) {
          this.form.controls.tableId.setValue(tables[0]?.id ?? 0);
        }
      },
      error: (err) => this.error.set(apiErrorMessage(err, 'No se pudieron consultar las mesas.')),
    });
  }

  quantityOf(itemId: number): number {
    return this.quantities()[itemId] || 0;
  }

  setQuantity(itemId: number, value: string): void {
    const quantity = Math.max(0, Math.floor(Number(value) || 0));
    this.quantities.update((q) => ({ ...q, [itemId]: quantity }));
  }

  isCurrentTable(table: TableResponseDTO): boolean {
    return this.currentTable?.id === table.id;
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const ordered = Object.entries(this.quantities()).filter(([, qty]) => qty > 0);

    const request: ReservationRequestDTO = {
      customerId: value.customerId,
      reservationDateTime: value.reservationDateTime,
      numberOfPeople: value.numberOfPeople,
      tableId: value.tableId,
      specialRequests: value.specialRequests || null,
      menuItemIds: ordered.map(([id]) => Number(id)),
      quantities: ordered.map(([, qty]) => qty),
    };

    this.saving.set(true);
    this.error.set(null);

    const id = this.reservationId();
    const request$ = id === null
      ? this.reservationService.create(request)
      : this.reservationService.update(id, request);

    request$.subscribe({
      next: () => this.router.navigate(['/reservas']),
      error: (err) => {
        this.error.set(apiErrorMessage(err, 'No se pudo guardar la reserva.'));
        this.saving.set(false);
      },
    });
  }
}
