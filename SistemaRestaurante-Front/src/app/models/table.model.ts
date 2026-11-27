export type TableStatus = 'Available' | 'Occupied' | 'Reserved' | 'OutOfService';

export interface TableResponseDTO {
  id: number;
  tableNumber: number;
  capacity: number;
  location?: string | null;
  status: TableStatus;
  restaurantId: number;
  restaurantName: string;
  isAvailable: boolean;
}

export interface TableRequestDTO {
  tableNumber: number;
  capacity: number;
  location?: string | null;
  status: TableStatus;
}
