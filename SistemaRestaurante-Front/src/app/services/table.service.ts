import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { TableResponseDTO, TableRequestDTO } from '../models/table.model';

@Injectable({
  providedIn: 'root'
})
export class TableService {
  private apiUrl = 'https://localhost:7191/api/Table'; // 👈 Tu URL real de Swagger

  constructor(private http: HttpClient) { }

  getTables(): Observable<TableResponseDTO[]> {
    return this.http.get<TableResponseDTO[]>(this.apiUrl);
  }

}