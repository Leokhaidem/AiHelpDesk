import { apiClient } from "@/lib/api-client";

export type CategoryRef = { id: number; name: string };

export type PublicCategory = {
  id: number;
  name: string;
  description?: string;
};

export type CategoryAgent = { id: string; fullName: string };

export type CategoryResponse = {
  id: number;
  name: string;
  description?: string;
  isActive: boolean;
  agents: CategoryAgent[];
  createdAt: string;
  updatedAt?: string;
};

export type CreateCategoryRequest = { name: string; description?: string };
export type UpdateCategoryRequest = { name: string; description?: string };

export const categoriesApi = {
  /** Active categories, available to anonymous callers (used at registration). */
  publicList: () => apiClient.get<PublicCategory[]>("/api/categories/public").then((r) => r.data),
  list: (activeOnly = false) =>
    apiClient
      .get<CategoryResponse[]>("/api/categories", { params: { activeOnly } })
      .then((r) => r.data),
  getById: (id: number) =>
    apiClient.get<CategoryResponse>(`/api/categories/${id}`).then((r) => r.data),
  create: (body: CreateCategoryRequest) =>
    apiClient.post<CategoryResponse>("/api/categories", body).then((r) => r.data),
  update: (id: number, body: UpdateCategoryRequest) =>
    apiClient.put<CategoryResponse>(`/api/categories/${id}`, body).then((r) => r.data),
  setStatus: (id: number, isActive: boolean) =>
    apiClient
      .patch<CategoryResponse>(`/api/categories/${id}/status`, { isActive })
      .then((r) => r.data),
};
