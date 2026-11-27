import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { Observable } from 'rxjs';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Allergen, MenuItemCategory, MenuItemRequestDTO } from '../../models/menu.model';
import { StationResponseDTO } from '../../models/order.model';
import { StationService } from '../../services/station.service';
import { MenuService } from '../../services/menu.service';
import { apiErrorMessage } from '../../utils/api-error';
import { ALLERGENS, ALLERGEN_LABELS, MENU_CATEGORIES, MENU_CATEGORY_LABELS } from '../../utils/labels';

@Component({
  selector: 'app-menu-form',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './menu-form.html',
})
export class MenuFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly menuService = inject(MenuService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly stationService = inject(StationService);

  readonly categories = MENU_CATEGORIES;
  readonly categoryLabels = MENU_CATEGORY_LABELS;
  readonly allergenOptions = ALLERGENS;
  readonly allergenLabels = ALLERGEN_LABELS;
  readonly stations = signal<StationResponseDTO[]>([]);

  readonly itemId = signal<number | null>(null);
  readonly isEditMode = computed(() => this.itemId() !== null);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    description: ['', Validators.maxLength(500)],
    price: [0, [Validators.required, Validators.min(0.01)]],
    category: ['MainCourse' as MenuItemCategory, Validators.required],
    isAvailable: [true],
    /** null = automática (Barra para bebidas, Cocina para el resto). */
    stationId: [null as number | null],
    allergens: [[] as Allergen[]],
  });

  ngOnInit(): void {
    this.stationService.getAll().subscribe({ next: (data) => this.stations.set(data) });
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      const id = Number(idParam);
      this.itemId.set(id);
      this.menuService.getMenuItemById(id).subscribe({
        next: (item) =>
          this.form.reset({
            name: item.name,
            description: item.description ?? '',
            price: item.price,
            category: item.category,
            isAvailable: item.isAvailable,
            stationId: item.stationId,
            allergens: item.allergens ?? [],
          }),
        error: (err) => this.error.set(apiErrorMessage(err, 'No se pudo cargar el plato.')),
      });
    }
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const request: MenuItemRequestDTO = { ...value, description: value.description || null };
    const id = this.itemId();

    this.saving.set(true);
    this.error.set(null);

    const request$: Observable<unknown> = id === null
      ? this.menuService.createMenuItem(request)
      : this.menuService.updateMenuItem(id, request);

    request$.subscribe({
      next: () => this.router.navigate(['/menu']),
      error: (err) => {
        this.error.set(apiErrorMessage(err, 'No se pudo guardar el plato.'));
        this.saving.set(false);
      },
    });
  }

  hasAllergen(a: Allergen): boolean {
    return this.form.controls.allergens.value.includes(a);
  }

  toggleAllergen(a: Allergen): void {
    const current = this.form.controls.allergens.value;
    this.form.controls.allergens.setValue(current.includes(a) ? current.filter((x) => x !== a) : [...current, a]);
  }
}
