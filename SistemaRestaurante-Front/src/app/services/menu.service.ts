import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { MenuItemCategory, MenuItemRequestDTO, MenuItemResponseDTO } from '../models/menu.model';

@Injectable({ providedIn: 'root' })
export class MenuService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/MenuItem`;

  /** Por defecto solo los platos disponibles; includeUnavailable=true trae todos. */
  getMenuItems(includeUnavailable = false): Observable<MenuItemResponseDTO[]> {
    const params = new HttpParams().set('includeUnavailable', includeUnavailable);
    return this.http.get<MenuItemResponseDTO[]>(this.apiUrl, { params });
  }

  getMenuItemById(id: number): Observable<MenuItemResponseDTO> {
    return this.http.get<MenuItemResponseDTO>(`${this.apiUrl}/${id}`);
  }

  getMenuItemsByCategory(category: MenuItemCategory): Observable<MenuItemResponseDTO[]> {
    return this.http.get<MenuItemResponseDTO[]>(`${this.apiUrl}/category/${category}`);
  }

  getMenuItemsByRestaurant(restaurantId: number): Observable<MenuItemResponseDTO[]> {
    return this.http.get<MenuItemResponseDTO[]>(`${this.apiUrl}/restaurant/${restaurantId}`);
  }

  createMenuItem(item: MenuItemRequestDTO): Observable<MenuItemResponseDTO> {
    return this.http.post<MenuItemResponseDTO>(this.apiUrl, item);
  }

  updateMenuItem(id: number, item: MenuItemRequestDTO): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, item);
  }

  deleteMenuItem(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
