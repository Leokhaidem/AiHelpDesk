import { apiClient } from "@/lib/api-client";

import type { CategoryRef } from "@/features/categories/api";

export type AdminRole = "Employee" | "Agent";

/** Admin user list / detail response (integration §10.7). */
export type AdminUser = {
  id: string;
  fullName: string;
  email: string;
  isActive: boolean;
  roles: string[];
  /** Set for users in the Agent role. */
  categories?: CategoryRef[];
  /** Assigned + InProgress ticket count. Set for users in the Agent role. */
  openTicketCount?: number;
};

export type CreateAdminUserRequest = {
  fullName: string;
  email: string;
  password: string;
  role: AdminRole;
  /** Required when role is Agent: the categories the new agent will work on. */
  categoryIds?: number[];
};

/** Flow I outcome — returned when deactivating / demoting an active Agent. */
export type AgentDeactivationResult = {
  /** Tickets moved from Assigned to AwaitingAssignment. */
  requeuedTicketCount: number;
  /** InProgress tickets left with the agent for manual review. */
  flaggedTicketCount: number;
  /** Tickets the post-deactivation sweep assigned to other agents. */
  autoAssignedTicketCount: number;
};

/** Body of PATCH /api/admin/users/{id}/status. */
export type UserStatusResult = {
  message: string;
  userId: string;
  isActive: boolean;
  agentDeactivation?: AgentDeactivationResult | null;
};

/** Body of PUT /api/admin/users/{id}/role. */
export type UserRoleResult = {
  message: string;
  userId: string;
  roles: string[];
  agentDeactivation?: AgentDeactivationResult | null;
};

export const usersApi = {
  /** All users, optionally filtered by role. Not paginated — client-side filtering. */
  list: (role?: AdminRole) =>
    apiClient
      .get<AdminUser[]>("/api/admin/users", { params: role ? { role } : undefined })
      .then((r) => r.data),
  getById: (id: string) => apiClient.get<AdminUser>(`/api/admin/users/${id}`).then((r) => r.data),
  create: (body: CreateAdminUserRequest) =>
    apiClient.post<AdminUser>("/api/admin/users", body).then((r) => r.data),
  update: (id: string, body: { fullName: string; email: string }) =>
    apiClient.put<AdminUser>(`/api/admin/users/${id}`, body).then((r) => r.data),
  setStatus: (id: string, isActive: boolean) =>
    apiClient.patch<UserStatusResult>(`/api/admin/users/${id}/status`, { isActive }).then((r) => r.data),
  setRole: (id: string, role: AdminRole, categoryIds?: number[]) =>
    apiClient.put<UserRoleResult>(`/api/admin/users/${id}/role`, { role, categoryIds }).then((r) => r.data),
  getCategories: (id: string) =>
    apiClient.get<CategoryRef[]>(`/api/admin/users/${id}/categories`).then((r) => r.data),
  setCategories: (id: string, categoryIds: number[]) =>
    apiClient
      .put<CategoryRef[]>(`/api/admin/users/${id}/categories`, { categoryIds })
      .then((r) => r.data),
};