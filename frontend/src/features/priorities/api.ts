import { apiClient } from "@/lib/api-client";

export type PriorityResponse = {
  id: number;
  name: string;
  level: number;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
};

export type CreatePriorityRequest = { name: string; level: number };
export type UpdatePriorityRequest = { name: string; level: number };

export const prioritiesApi = {
  /** Active priorities — open to every authenticated user (ticket filters, forms). */
  publicList: () =>
    apiClient
      .get<PriorityResponse[]>("/api/priorities", { params: { activeOnly: true } })
      .then((r) => r.data),
  list: (activeOnly = false) =>
    apiClient
      .get<PriorityResponse[]>("/api/priorities", { params: { activeOnly } })
      .then((r) => r.data),
  create: (body: CreatePriorityRequest) =>
    apiClient.post<PriorityResponse>("/api/priorities", body).then((r) => r.data),
  update: (id: number, body: UpdatePriorityRequest) =>
    apiClient.put<PriorityResponse>(`/api/priorities/${id}`, body).then((r) => r.data),
  setStatus: (id: number, isActive: boolean) =>
    apiClient
      .patch<PriorityResponse>(`/api/priorities/${id}/status`, { isActive })
      .then((r) => r.data),
};
