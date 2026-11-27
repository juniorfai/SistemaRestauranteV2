import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { StationRequestDTO, StationResponseDTO } from '../models/order.model';

/** Zonas de preparación (Cocina, Barra...). */
@Injectable({ providedIn: 'root' })
export class StationService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/Station`;

  getAll(): Observable<StationResponseDTO[]> {
    return this.http.get<StationResponseDTO[]>(this.apiUrl);
  }

  create(dto: StationRequestDTO): Observable<StationResponseDTO> {
    return this.http.post<StationResponseDTO>(this.apiUrl, dto);
  }

  update(id: number, dto: StationRequestDTO): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, dto);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
