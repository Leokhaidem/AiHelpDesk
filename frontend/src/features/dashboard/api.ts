import { apiClient } from "@/lib/api-client";

/**
 * GET /api/dashboard response (integration §10.12). All counts are
 * service-wide for Admin and agent-scoped for Agent — no scope param.
 */
export type DashboardMetrics = {
  totalTickets: number;
  created: number;
  analysisInProgress: number;
  analysisFailed: number;
  awaitingAssignment: number;
  assigned: number;
  inProgress: number;
  resolved: number;
  closed: number;
  unassigned: number;
  byCategory: { categoryName: string; count: number }[];
  byPriority: { priorityName: string; count: number }[];
};

export const dashboardApi = {
  get: () => apiClient.get<DashboardMetrics>("/api/dashboard").then((response) => response.data),
};
