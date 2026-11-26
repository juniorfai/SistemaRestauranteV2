import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { Observable } from 'rxjs';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MenuItemCategory, MenuItemRequestDTO } from '../../models/menu.model';
import { RestaurantResponseDTO } from '../../models/restaurant.model';
import { MenuService } from '../../services/menu.service';
import { RestaurantService } from '../../services/restaurant.service';
import { apiErrorMessage } from '../../utils/api-error';
import { MENU_CATEGORIES, MENU_CATEGORY_LABELS } from '../../utils/labels';

@Component({
  selector: 'app-menu-form',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './menu-form.html',
})
export class MenuFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly menuService = inject(MenuService);
  private readonly restaurantService = inject(RestaurantService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly categories = MENU_CATEGORIES;
  readonly categoryLabels = MENU_CATEGORY_LABELS;

  readonly itemId = signal<number | null>(null);
  readonly isEditMode = computed(() => this.itemId() !== null);
  readonly restaurants = signal<RestaurantResponseDTO[]>([]);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    description: ['', Validators.maxLength(500)],
    price: [0, [Validators.required, Validators.min(0.01)]],
    category: ['MainCourse' as MenuItemCategory, Validators.required],
    isAvailable: [true],
    restaurantId: [0, [Validators.required, Validators.min(1)]],
  });

  ngOnInit(): void {
    this.restaurantService.getRestaurants().subscribe({
      next: (data) => {
        this.restaurants.set(data);
        if (!this.isEditMode() && data.length > 0 && this.form.controls.restaurantId.value === 0) {
          this.form.controls.restaurantId.setValue(data[0].id);
        }
      },
    });

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
            restaurantId: item.restaurantId,
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
}
