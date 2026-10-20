import { apiClient } from "@/lib/api-client";

import type { CategoryRef } from "@/features/categories/api";
import type { PagedResult } from "@/types/api";

export type ApplicationStatus = "Pending" | "Approved" | "Rejected" | "Withdrawn";

export type AgentApplicationResponse = {
  id: number;
  userId: string;
  /** PascalCase string on the wire (integration doc section 7). */
  status: ApplicationStatus;
  message?: string;
  categories: CategoryRef[];
  createdAt: string;
  reviewedAt?: string;
  reviewNote?: string;
};

export type AdminAgentApplicationResponse = AgentApplicationResponse & {
  applicantName: string;
  applicantEmail: string;
};

export type CreateApplicationRequest = {
  categoryIds: number[];
  message?: string;
};

export type ReviewApplicationRequest = {
  categoryIds?: number[];
  note?: string;
};

export const agentApplicationsApi = {
  create: (body: CreateApplicationRequest) =>
    apiClient.post<AgentApplicationResponse>("/api/agent-applications", body).then((r) => r.data),
  mine: () =>
    apiClient.get<AgentApplicationResponse>("/api/agent-applications/mine").then((r) => r.data),
  withdraw: () =>
    apiClient.delete<{ message: string }>("/api/agent-applications/mine").then((r) => r.data),
};

export const adminApplicationsApi = {
  list: (params: { status: string; page: number; pageSize: number }) =>
    apiClient
      .get<PagedResult<AdminAgentApplicationResponse>>("/api/admin/agent-applications", { params })
      .then((r) => r.data),
  approve: (id: number, body?: ReviewApplicationRequest) =>
    apiClient
      .post<AdminAgentApplicationResponse>(`/api/admin/agent-applications/${id}/approve`, body)
      .then((r) => r.data),
  reject: (id: number, body?: ReviewApplicationRequest) =>
    apiClient
      .post<AdminAgentApplicationResponse>(`/api/admin/agent-applications/${id}/reject`, body)
      .then((r) => r.data),
};
