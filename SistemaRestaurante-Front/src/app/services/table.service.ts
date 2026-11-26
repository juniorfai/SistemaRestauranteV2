import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { TableRequestDTO, TableResponseDTO, TableStatus } from '../models/table.model';

@Injectable({ providedIn: 'root' })
export class TableService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/Table`;

  getTables(): Observable<TableResponseDTO[]> {
    return this.http.get<TableResponseDTO[]>(this.apiUrl);
  }

  /** Mesas libres para esa fecha/hora ("yyyy-MM-ddTHH:mm") y número de personas. */
  getAvailable(dateTime: string, numberOfPeople: number): Observable<TableResponseDTO[]> {
    const params = new HttpParams().set('dateTime', dateTime).set('numberOfPeople', numberOfPeople);
    return this.http.get<TableResponseDTO[]>(`${this.apiUrl}/available`, { params });
  }

  createTable(table: TableRequestDTO): Observable<TableResponseDTO> {
    return this.http.post<TableResponseDTO>(this.apiUrl, table);
  }

  updateTable(id: number, table: TableRequestDTO): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, table);
  }

  updateStatus(id: number, status: TableStatus): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/${id}/status`, { status });
  }

  deleteTable(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
