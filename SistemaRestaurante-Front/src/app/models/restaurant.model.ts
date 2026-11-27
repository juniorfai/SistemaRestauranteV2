export interface RestaurantResponseDTO {
  id: number;
  name: string;
  address: string;
  phoneNumber: string;
  email: string;
  openingHours?: string | null;
  isActive: boolean;
  createdAt: string;
}

export interface RestaurantRequestDTO {
  name: string;
  address: string;
  phoneNumber: string;
  email: string;
  openingHours?: string | null;
}

/** Alta de restaurante junto con su primer administrador. */
export interface RestaurantCreateRequestDTO extends RestaurantRequestDTO {
  adminUsername: string;
  adminFullName: string;
  adminPassword: string;
}
