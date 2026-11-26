import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { AuthService } from '../../services/auth.service';
import { CurrencyPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MenuItemCategory, MenuItemResponseDTO } from '../../models/menu.model';
import { MenuService } from '../../services/menu.service';
import { apiErrorMessage } from '../../utils/api-error';
import { MENU_CATEGORIES, MENU_CATEGORY_LABELS } from '../../utils/labels';

@Component({
  selector: 'app-menu-list',
  standalone: true,
  imports: [CurrencyPipe, RouterLink],
  templateUrl: './menu-list.html',
})
export class MenuListComponent implements OnInit {
  readonly auth = inject(AuthService);
  private readonly menuService = inject(MenuService);

  readonly categories = MENU_CATEGORIES;
  readonly categoryLabels = MENU_CATEGORY_LABELS;

  readonly items = signal<MenuItemResponseDTO[]>([]);
  readonly selectedCategory = signal<MenuItemCategory | null>(null);
  readonly includeUnavailable = signal(false);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  readonly visibleItems = computed(() => {
    const category = this.selectedCategory();
    return category ? this.items().filter((i) => i.category === category) : this.items();
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.menuService.getMenuItems(this.includeUnavailable()).subscribe({
      next: (data) => {
        this.items.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(apiErrorMessage(err, 'No se pudo cargar la carta.'));
        this.loading.set(false);
      },
    });
  }

  toggleUnavailable(): void {
    this.includeUnavailable.update((v) => !v);
    this.load();
  }

  remove(item: MenuItemResponseDTO): void {
    if (!confirm(`¿Eliminar "${item.name}" de la carta?`)) {
      return;
    }
    this.menuService.deleteMenuItem(item.id).subscribe({
      next: () => this.load(),
      error: (err) => this.error.set(apiErrorMessage(err)),
    });
  }
}
