import { Component, OnInit } from '@angular/core';
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

  constructor(private tableService: TableService) {}

  ngOnInit(): void {
    this.tableService.getTables().subscribe({
      next: (datos) => {
        this.listaMesas = datos;
      },
      error: (err) => {
        console.error('Error al conectar con la API de Mesas:', err);
      }
    });
  }
}