/** PlatformAdmin = quien vende el programa: da de alta restaurantes, no trabaja en ninguno. */
export type UserRole = 'Admin' | 'Manager' | 'Staff' | 'PlatformAdmin';

/** Roles que puede tener el personal de un restaurante. */
export type RestaurantRole = Exclude<UserRole, 'PlatformAdmin'>;

export interface UserResponseDTO {
  id: number;
  username: string;
  fullName: string;
  role: UserRole;
  isActive: boolean;
  lastLoginAt?: string | null;
  createdAt: string;
  /** null para el administrador de la plataforma. */
  restaurantId?: number | null;
  restaurantName?: string | null;
}

export interface LoginRequestDTO {
  username: string;
  password: string;
}

export interface LoginResponseDTO {
  token: string;
  /** Fecha UTC en ISO. */
  expiresAt: string;
  user: UserResponseDTO;
}

export interface UserCreateRequestDTO {
  username: string;
  fullName: string;
  role: RestaurantRole;
  password: string;
}

export interface UserUpdateRequestDTO {
  fullName: string;
  role: RestaurantRole;
  isActive: boolean;
}

export interface ChangePasswordRequestDTO {
  currentPassword: string;
  newPassword: string;
}
