import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  AddOrderItemDTO,
  OpenOrderRequestDTO,
  OrderCourse,
  OrderItemResponseDTO,
  OrderItemStatus,
  OrderResponseDTO,
  UpdateOrderItemDTO,
} from '../models/order.model';

/** Cuentas de las mesas (servicio en sala). */
@Injectable({ providedIn: 'root' })
export class OrderService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/Order`;

  getOpen(): Observable<OrderResponseDTO[]> {
    return this.http.get<OrderResponseDTO[]>(`${this.apiUrl}/open`);
  }

  getById(id: number): Observable<OrderResponseDTO> {
    return this.http.get<OrderResponseDTO>(`${this.apiUrl}/${id}`);
  }

  open(dto: OpenOrderRequestDTO): Observable<OrderResponseDTO> {
    return this.http.post<OrderResponseDTO>(this.apiUrl, dto);
  }

  addItems(orderId: number, items: AddOrderItemDTO[]): Observable<OrderResponseDTO> {
    return this.http.post<OrderResponseDTO>(`${this.apiUrl}/${orderId}/items`, { items });
  }

  updateDraft(orderId: number, itemId: number, dto: UpdateOrderItemDTO): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${orderId}/items/${itemId}`, dto);
  }

  removeDraft(orderId: number, itemId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${orderId}/items/${itemId}`);
  }

  /** Envía a cocina lo apuntado; con holdFromCourse, ese tiempo y los siguientes esperan. */
  send(orderId: number, holdFromCourse: OrderCourse | null): Observable<OrderResponseDTO> {
    return this.http.post<OrderResponseDTO>(`${this.apiUrl}/${orderId}/send`, { holdFromCourse });
  }

  /** "Marchar": libera los platos retenidos hasta ese tiempo. */
  fire(orderId: number, course: OrderCourse): Observable<OrderResponseDTO> {
    return this.http.post<OrderResponseDTO>(`${this.apiUrl}/${orderId}/fire`, { course });
  }

  setItemStatus(orderId: number, itemId: number, status: OrderItemStatus, expectedStatus?: OrderItemStatus): Observable<OrderItemResponseDTO> {
    return this.http.put<OrderItemResponseDTO>(`${this.apiUrl}/${orderId}/items/${itemId}/status`, { status, expectedStatus });
  }

  cancelItem(orderId: number, itemId: number, reason: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${orderId}/items/${itemId}/cancel`, { reason });
  }

  moveToTable(orderId: number, tableId: number): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${orderId}/table`, { tableId });
  }

  setGuests(orderId: number, guests: number): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${orderId}/guests`, { guests });
  }

  cancel(orderId: number, reason: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${orderId}/cancel`, { reason });
  }
}
