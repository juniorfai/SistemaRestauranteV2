import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { KitchenTicketDTO, OrderItemResponseDTO, OrderItemStatus } from '../models/order.model';

/** Pantalla de cocina y barra. */
@Injectable({ providedIn: 'root' })
export class KitchenService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/Kitchen`;

  getTickets(stationId: number | null): Observable<KitchenTicketDTO[]> {
    const query = stationId ? `?stationId=${stationId}` : '';
    return this.http.get<KitchenTicketDTO[]>(`${this.apiUrl}/tickets${query}`);
  }

  /** expectedStatus: si otra pantalla ya lo cambió, la API responde 409 y no pisa nada. */
  setItemStatus(orderId: number, itemId: number, status: OrderItemStatus, expectedStatus: OrderItemStatus): Observable<OrderItemResponseDTO> {
    return this.http.put<OrderItemResponseDTO>(`${this.apiUrl}/orders/${orderId}/items/${itemId}/status`, { status, expectedStatus });
  }

  /** Despacha la comanda: todo lo pendiente de esa zona queda listo. */
  bump(orderId: number, stationId: number): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/orders/${orderId}/stations/${stationId}/bump`, {});
  }
}
