import { Component, OnInit, ChangeDetectorRef } from '@angular/core'; // 1. Importamos ChangeDetectorRef
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { TableService } from '../../services/table.service';
import { TableResponseDTO } from '../../models/table.model';

@Component({
  selector: 'app-reservation-list',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './reservation-list.component.html'
})
export class ReservationListComponent implements OnInit {
  listaMesas: TableResponseDTO[] = [];

  // 2. Inyectamos cdr en el constructor
  constructor(
    private tableService: TableService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.cargarMesas();
  }

  cargarMesas(): void {
    this.tableService.getTables().subscribe({
      next: (datos) => {
        this.listaMesas = datos;
        // 3. Notificamos a Angular para pintar las mesas inmediatamente
        this.cdr.detectChanges(); 
      },
      error: (err) => {
        console.error('Error al conectar con la API de Mesas:', err);
      }
    });
  }
}