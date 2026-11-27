export type MenuItemCategory = 'Appetizer' | 'MainCourse' | 'Dessert' | 'Drink' | 'SideDish';

export interface MenuItemResponseDTO {
  id: number;
  name: string;
  description?: string | null;
  price: number;
  category: MenuItemCategory;
  categoryName: string;
  isAvailable: boolean;
  restaurantId: number;
}

export interface MenuItemRequestDTO {
  name: string;
  description?: string | null;
  price: number;
  category: MenuItemCategory;
  isAvailable: boolean;
}
