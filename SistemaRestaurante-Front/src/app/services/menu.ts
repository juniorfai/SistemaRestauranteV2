import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { MenuItemResponseDTO } from '../models/menu.model';

@Injectable({
  providedIn: 'root'
})
export class MenuService {
  // Asegúrate de que este puerto sea idéntico al de tus otros servicios activos
  private apiUrl = 'https://localhost:7191/api/MenuItem';

  constructor(private http: HttpClient) {}

  // 1. Obtener todos los platillos del menú
  getMenuItems(): Observable<MenuItemResponseDTO[]> {
    return this.http.get<MenuItemResponseDTO[]>(this.apiUrl);
  }

  // 2. Filtrar platillos por categoría (Ej: Entradas, Bebidas, Fuertes)
  getMenuItemsByCategory(category: string): Observable<MenuItemResponseDTO[]> {
    return this.http.get<MenuItemResponseDTO[]>(`${this.apiUrl}/category/${category}`);
  }

  // 3. Obtener platillos asociados a un restaurante específico
  getMenuItemsByRestaurant(restaurantId: number): Observable<MenuItemResponseDTO[]> {
    return this.http.get<MenuItemResponseDTO[]>(`${this.apiUrl}/restaurant/${restaurantId}`);
  }
}