export interface CustomerResponseDTO {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  preferences?: string;
  isActive: boolean;
}

export interface CustomerRequestDTO {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  preferences?: string;
}