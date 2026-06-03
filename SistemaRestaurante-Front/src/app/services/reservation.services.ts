import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ReservationResponseDTO, ReservationRequestDTO } from '../models/reservation.model';

@Injectable({
  providedIn: 'root' // Hace que este servicio se pueda inyectar en cualquier pantalla
})
export class ReservationService {
  // 🚨 Esta es la URL exacta de tu Swagger para Reservas
  private apiUrl = 'https://localhost:7191/api/Table'; 

  constructor(private http: HttpClient) { }

  // GET: Traer todas las reservas de la BD
  getReservations(): Observable<ReservationResponseDTO[]> {
    return this.http.get<ReservationResponseDTO[]>(this.apiUrl);
  }

  // POST: Enviar una nueva reserva hecha en el formulario hacia el Backend
  createReservation(reservation: ReservationRequestDTO): Observable<ReservationResponseDTO> {
    return this.http.post<ReservationResponseDTO>(this.apiUrl, reservation);
  }
}