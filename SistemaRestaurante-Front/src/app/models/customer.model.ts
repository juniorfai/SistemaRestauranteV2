export interface CustomerResponseDTO {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  preferences?: string | null;
  totalReservations: number;
  createdAt: string;
}

export interface CustomerRequestDTO {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  preferences?: string | null;
}
