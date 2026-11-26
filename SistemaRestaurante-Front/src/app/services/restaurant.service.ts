import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { RestaurantResponseDTO } from '../models/restaurant.model';

@Injectable({ providedIn: 'root' })
export class RestaurantService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/Restaurant`;

  getRestaurants(): Observable<RestaurantResponseDTO[]> {
    return this.http.get<RestaurantResponseDTO[]>(this.apiUrl);
  }
}
