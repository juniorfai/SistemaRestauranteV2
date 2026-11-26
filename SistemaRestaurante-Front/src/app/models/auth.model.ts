export type UserRole = 'Admin' | 'Manager' | 'Staff';

export interface UserResponseDTO {
  id: number;
  username: string;
  fullName: string;
  role: UserRole;
  isActive: boolean;
  lastLoginAt?: string | null;
  createdAt: string;
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
  role: UserRole;
  password: string;
}

export interface UserUpdateRequestDTO {
  fullName: string;
  role: UserRole;
  isActive: boolean;
}

export interface ChangePasswordRequestDTO {
  currentPassword: string;
  newPassword: string;
}
