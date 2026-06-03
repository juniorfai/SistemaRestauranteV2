import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CustomerResponseDTO, CustomerRequestDTO } from '../models/customer.model';

@Injectable({
  providedIn: 'root'
})
export class CustomerService {
  // Asegúrate de que este puerto coincida exactamente con tu backend en ejecución
  private apiUrl = 'https://localhost:7191/api/Customer'; 

  constructor(private http: HttpClient) {}

  // Obtener todos los clientes (GET)
  getCustomers(): Observable<CustomerResponseDTO[]> {
    return this.http.get<CustomerResponseDTO[]>(this.apiUrl);
  }

  // Obtener un cliente por ID (GET)
  getCustomerById(id: number): Observable<CustomerResponseDTO> {
    return this.http.get<CustomerResponseDTO>(`${this.apiUrl}/${id}`);
  }

  // Crear cliente (POST)
  createCustomer(customer: CustomerRequestDTO): Observable<CustomerResponseDTO> {
    return this.http.post<CustomerResponseDTO>(this.apiUrl, customer);
  }

  // Editar cliente (PUT)
  updateCustomer(id: number, customer: CustomerRequestDTO): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, customer);
  }

  // Eliminar cliente (DELETE)
  deleteCustomer(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}