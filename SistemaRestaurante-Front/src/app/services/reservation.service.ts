import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ReservationRequestDTO, ReservationResponseDTO } from '../models/reservation.model';

export type ReservationAction = 'confirm' | 'cancel' | 'complete' | 'no-show';

@Injectable({ providedIn: 'root' })
export class ReservationService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/Reservation`;

  /** Reservas pendientes o confirmadas desde hoy en adelante. */
  getUpcoming(): Observable<ReservationResponseDTO[]> {
    return this.http.get<ReservationResponseDTO[]>(this.apiUrl);
  }

  /** Todas las reservas de un día. date en formato yyyy-MM-dd. */
  getByDate(date: string): Observable<ReservationResponseDTO[]> {
    return this.http.get<ReservationResponseDTO[]>(`${this.apiUrl}/date/${date}`);
  }

  getByCustomer(customerId: number): Observable<ReservationResponseDTO[]> {
    return this.http.get<ReservationResponseDTO[]>(`${this.apiUrl}/customer/${customerId}`);
  }

  getById(id: number): Observable<ReservationResponseDTO> {
    return this.http.get<ReservationResponseDTO>(`${this.apiUrl}/${id}`);
  }

  create(reservation: ReservationRequestDTO): Observable<ReservationResponseDTO> {
    return this.http.post<ReservationResponseDTO>(this.apiUrl, reservation);
  }

  update(id: number, reservation: ReservationRequestDTO): Observable<ReservationResponseDTO> {
    return this.http.put<ReservationResponseDTO>(`${this.apiUrl}/${id}`, reservation);
  }

  changeStatus(id: number, action: ReservationAction): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/${id}/${action}`, null);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
