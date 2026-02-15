export const CategoryType = {
  Income: 1,
  Expense: 2,
} as const;

export type CategoryType = (typeof CategoryType)[keyof typeof CategoryType];

export interface CategoryResponse {
  id: string;
  name: string;
  type: CategoryType;
  createdAt: string;
  updatedAt: string | null;
}

export interface CreateCategoryRequest {
  name: string;
  type: CategoryType;
}

export interface UpdateCategoryRequest {
  name: string;
}
