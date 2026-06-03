import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { ReservationService } from '../../services/reservation.services';
import { ReservationRequestDTO } from '../../models/reservation.model';


@Component({
  selector: 'app-reservation-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './reservation-form.component.html'
})
export class ReservationFormComponent implements OnInit {
  reservaForm!: FormGroup;

  constructor(
    private fb: FormBuilder,
    private reservationService: ReservationService,
    private router: Router
  ) {}

  ngOnInit(): void {
    // Definimos las reglas de validación para proteger el backend
    this.reservaForm = this.fb.group({
      customerId: ['', [Validators.required, Validators.min(1)]],
      tableId: ['', [Validators.required, Validators.min(1)]],
      reservationDateTime: ['', Validators.required],
      numberOfPeople: ['', [Validators.required, Validators.min(1), Validators.max(20)]],
      specialRequests: ['']
    });
  }

  guardarReserva(): void {
    if (this.reservaForm.invalid) {
      this.reservaForm.markAllAsTouched(); // Muestra alertas rojas si faltan campos
      return;
    }

    const nuevaReserva: ReservationRequestDTO = this.reservaForm.value;

    this.reservationService.createReservation(nuevaReserva).subscribe({
      next: () => {
        alert('¡Reserva registrada con éxito!');
        this.router.navigate(['/reservas']); // Redirige automáticamente a la tabla principal
      },
      error: (err) => {
        console.error(err);
        alert('Error al guardar en la base de datos.');
      }
    });
  }
}