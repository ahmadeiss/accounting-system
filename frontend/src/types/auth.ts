// Mirrors Accounting.Application.Auth.DTOs

export interface UserProfile {
  id: string;
  username: string;
  email: string;
  fullName: string;
  roleName: string;
  branchId: string | null;
  permissions: string[];
}

export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string; // ISO date string
  user: UserProfile;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

