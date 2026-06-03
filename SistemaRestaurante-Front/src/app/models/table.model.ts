export interface TableResponseDTO {
  id: number;
  number: number;
  capacity: number;
  status: string; // O el tipo de dato que devuelva tu API (ej: numerico o texto)
  restaurantId?: number;
}

export interface TableRequestDTO {
  number: number;
  capacity: number;
  restaurantId?: number;
}