import { apiClient } from "@/lib/api-client";

import type { CategoryRef } from "@/features/categories/api";

/** The own agent work profile (integration §10.5). */
export type AgentProfileResponse = {
  id: string;
  fullName: string;
  email: string;
  isAvailable: boolean;
  /** Capacity cap — how many Assigned + InProgress tickets the agent holds. */
  maxOpenTickets: number;
  /** Assigned + InProgress ticket count. */
  openTicketCount: number;
  categories: CategoryRef[];
};

export const agentProfileApi = {
  me: () => apiClient.get<AgentProfileResponse>("/api/agent/me").then((r) => r.data),
  /** Toggle availability and/or adjust the capacity cap; triggers an assignment sweep. */
  setAvailability: (body: { isAvailable: boolean; maxOpenTickets?: number }) =>
    apiClient
      .put<AgentProfileResponse>("/api/agent/me/availability", body)
      .then((r) => r.data),
  /** Replace the categories the agent works on (at least one required). */
  setCategories: (categoryIds: number[]) =>
    apiClient.put<CategoryRef[]>("/api/agent/me/categories", { categoryIds }).then((r) => r.data),
};