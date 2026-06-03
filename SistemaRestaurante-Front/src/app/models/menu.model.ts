export interface MenuItemResponseDTO {
  id: number;
  name: string;
  description: string;
  price: number;
  category: string;
  imageUrl?: string;
  restaurantId: number;
  isActive: boolean;
}