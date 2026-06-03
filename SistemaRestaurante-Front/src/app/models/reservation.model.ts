export interface ReservationResponseDTO {
  id: number;
  reservationDateTime: string;
  numberOfPeople: number;
  status: number; 
  specialRequests?: string;
  totalAmount?: number;
  customerId: number;
  tableId: number;
  isActive: boolean;
}

export interface ReservationRequestDTO {
  reservationDateTime: string;
  numberOfPeople: number;
  specialRequests?: string;
  customerId: number;
  tableId: number;
}