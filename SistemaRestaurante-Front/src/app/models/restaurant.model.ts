export interface RestaurantResponseDTO {
  id: number;
  name: string;
  address: string;
  phoneNumber: string;
  email: string;
  openingHours?: string | null;
}
