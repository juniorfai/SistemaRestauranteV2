import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  RestaurantCreateRequestDTO,
  RestaurantRequestDTO,
  RestaurantResponseDTO,
} from '../models/restaurant.model';

@Injectable({ providedIn: 'root' })
export class RestaurantService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/Restaurant`;

  /** Todos los restaurantes (solo el administrador de la plataforma). */
  getRestaurants(): Observable<RestaurantResponseDTO[]> {
    return this.http.get<RestaurantResponseDTO[]>(this.apiUrl);
  }

  /** Restaurante del usuario conectado. */
  getCurrent(): Observable<RestaurantResponseDTO> {
    return this.http.get<RestaurantResponseDTO>(`${this.apiUrl}/current`);
  }

  create(dto: RestaurantCreateRequestDTO): Observable<RestaurantResponseDTO> {
    return this.http.post<RestaurantResponseDTO>(this.apiUrl, dto);
  }

  update(id: number, dto: RestaurantRequestDTO): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, dto);
  }

  /** Suspende (false) o reactiva (true) un restaurante. */
  setActive(id: number, isActive: boolean): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/${id}/active`, { isActive });
  }
}
