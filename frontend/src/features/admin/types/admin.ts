export const RoleType = {
  Admin: 'Admin',
  Member: 'Member',
} as const;

export type RoleType = (typeof RoleType)[keyof typeof RoleType];

export interface UserResponse {
  id: string;
  name: string;
  email: string;
  role: RoleType;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface CreateUserRequest {
  name: string;
  email: string;
  password: string;
  role: RoleType;
  operationId?: string;
}
