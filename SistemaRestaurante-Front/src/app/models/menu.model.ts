export type MenuItemCategory = 'Appetizer' | 'MainCourse' | 'Dessert' | 'Drink' | 'SideDish';

/** Los 14 alérgenos de declaración obligatoria. */
export type Allergen =
  | 'Gluten' | 'Crustaceans' | 'Eggs' | 'Fish' | 'Peanuts' | 'Soy' | 'Milk'
  | 'TreeNuts' | 'Celery' | 'Mustard' | 'Sesame' | 'Sulphites' | 'Lupin' | 'Molluscs';

export interface MenuItemResponseDTO {
  id: number;
  name: string;
  description?: string | null;
  price: number;
  category: MenuItemCategory;
  categoryName: string;
  isAvailable: boolean;
  restaurantId: number;
  stationId: number;
  stationName: string;
  allergens: Allergen[];
}

export interface MenuItemRequestDTO {
  name: string;
  description?: string | null;
  price: number;
  category: MenuItemCategory;
  isAvailable: boolean;
  /** Sin indicar: Barra para bebidas, Cocina para el resto. */
  stationId?: number | null;
  allergens: Allergen[];
}
