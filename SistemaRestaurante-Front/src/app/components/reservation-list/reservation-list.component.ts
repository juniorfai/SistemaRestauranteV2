import { Component, OnInit, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ReservationResponseDTO } from '../../models/reservation.model';
import { ReservationAction, ReservationService } from '../../services/reservation.service';
import { apiErrorMessage } from '../../utils/api-error';
import {
  RESERVATION_STATUS_BADGES,
  RESERVATION_STATUS_LABELS,
  toDateInputValue,
} from '../../utils/labels';

const ACTION_CONFIRMATIONS: Record<ReservationAction, string> = {
  confirm: '¿Confirmar esta reserva?',
  cancel: '¿Cancelar esta reserva?',
  complete: '¿Marcar esta reserva como completada?',
  'no-show': '¿Marcar que el cliente no asistió?',
};

@Component({
  selector: 'app-reservation-list',
  standalone: true,
  imports: [DatePipe, CurrencyPipe, FormsModule, RouterLink],
  templateUrl: './reservation-list.component.html',
})
export class ReservationListComponent implements OnInit {
  private readonly reservationService = inject(ReservationService);

  readonly statusLabels = RESERVATION_STATUS_LABELS;
  readonly statusBadges = RESERVATION_STATUS_BADGES;

  readonly reservations = signal<ReservationResponseDTO[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  /** 'upcoming' = próximas reservas abiertas; 'day' = todas las del día elegido. */
  readonly mode = signal<'upcoming' | 'day'>('upcoming');
  selectedDate = toDateInputValue(new Date());

  ngOnInit(): void {
    this.load();
  }

  showUpcoming(): void {
    this.mode.set('upcoming');
    this.load();
  }

  showDay(): void {
    this.mode.set('day');
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);

    const request =
      this.mode() === 'upcoming'
        ? this.reservationService.getUpcoming()
        : this.reservationService.getByDate(this.selectedDate);

    request.subscribe({
      next: (data) => {
        this.reservations.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(apiErrorMessage(err, 'No se pudieron cargar las reservas.'));
        this.loading.set(false);
      },
    });
  }

  runAction(reservation: ReservationResponseDTO, action: ReservationAction): void {
    if (!confirm(ACTION_CONFIRMATIONS[action])) {
      return;
    }

    this.reservationService.changeStatus(reservation.id, action).subscribe({
      next: () => this.load(),
      error: (err) => this.error.set(apiErrorMessage(err)),
    });
  }

  isOpen(reservation: ReservationResponseDTO): boolean {
    return reservation.status === 'Pending' || reservation.status === 'Confirmed';
  }
}
