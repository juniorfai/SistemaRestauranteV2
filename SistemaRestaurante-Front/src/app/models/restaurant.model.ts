export interface RestaurantResponseDTO {
  id: number;
  name: string;
  address: string;
  phoneNumber: string;
  email: string;
  openingHours?: string | null;
  /** ISO 4217, p. ej. "COP". */
  currencyCode: string;
  /** Propina sugerida en la cuenta (%). 0 = no sugerir. */
  suggestedTipPercent: number;
  isActive: boolean;
  createdAt: string;
}

export interface RestaurantRequestDTO {
  name: string;
  address: string;
  phoneNumber: string;
  email: string;
  openingHours?: string | null;
  currencyCode: string;
  suggestedTipPercent: number;
}

/** Alta de restaurante junto con su primer administrador. */
export interface RestaurantCreateRequestDTO extends RestaurantRequestDTO {
  adminUsername: string;
  adminFullName: string;
  adminPassword: string;
}
