import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { CustomerRequestDTO, CustomerResponseDTO } from '../models/customer.model';

@Injectable({ providedIn: 'root' })
export class CustomerService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/Customer`;

  getCustomers(): Observable<CustomerResponseDTO[]> {
    return this.http.get<CustomerResponseDTO[]>(this.apiUrl);
  }

  getCustomerById(id: number): Observable<CustomerResponseDTO> {
    return this.http.get<CustomerResponseDTO>(`${this.apiUrl}/${id}`);
  }

  createCustomer(customer: CustomerRequestDTO): Observable<CustomerResponseDTO> {
    return this.http.post<CustomerResponseDTO>(this.apiUrl, customer);
  }

  updateCustomer(id: number, customer: CustomerRequestDTO): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, customer);
  }

  deleteCustomer(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
