import { apiClient } from "@/lib/api-client";

import {
  TICKET_STATUSES,
  type MessageResponse,
  type PagedResult,
  type TicketEventType,
  type TicketStatus,
} from "@/types/api";

/** Human-readable labels for the audit timeline (numeric event types). */
export const TICKET_EVENT_TYPE_LABELS: Record<TicketEventType, string> = {
  0: "Ticket created",
  1: "Status changed",
  2: "Agent assigned",
  3: "AI analysis succeeded",
  4: "AI analysis failed",
  5: "AI analysis retried",
  6: "Comment added",
  7: "Attachment added",
  8: "Ticket resolved",
  9: "Ticket closed",
  10: "Category overridden",
  11: "Priority overridden",
};

export type TicketSummaryResponse = {
  id: number;
  title: string;
  status: TicketStatus;
  categoryName?: string;
  priorityName?: string;
  assignedAgentId?: string;
  assignedAgentName?: string;
  createdAt: string;
  updatedAt?: string;
};

export type TicketDetailResponse = {
  id: number;
  title: string;
  description: string;
  status: TicketStatus;
  aiSummary?: string | null;
  aiSuggestedResolution?: string | null;
  categoryId?: number | null;
  categoryName?: string;
  priorityId?: number | null;
  priorityName?: string;
  employeeId: string;
  employeeName?: string;
  assignedAgentId?: string;
  assignedAgentName?: string;
  resolution?: string | null;
  createdAt: string;
  updatedAt?: string;
  resolvedAt?: string | null;
  closedAt?: string | null;
};

export type CommentResponse = {
  id: number;
  ticketId: number;
  authorId: string;
  authorName?: string;
  content: string;
  createdAt: string;
  updatedAt?: string;
};

export type TicketHistoryEvent = {
  id: string;
  ticketId: number;
  eventType: TicketEventType;
  oldValue?: string;
  newValue?: string;
  actorId?: string;
  timestamp: string;
  details?: string;
};

/** Query params accepted by all three scoped list endpoints. */
export type TicketListParams = {
  page?: number;
  pageSize?: number;
  status?: TicketStatus;
  categoryId?: number;
  priorityId?: number;
  assignedAgentId?: string;
  employeeId?: string;
  fromDate?: string;
  toDate?: string;
  agentInactive?: boolean;
};

/** Validates a URL search param against the TicketStatus union. */
export function toTicketStatus(value: string | undefined): TicketStatus | undefined {
  if (value && (TICKET_STATUSES as readonly string[]).includes(value)) {
    return value as TicketStatus;
  }
  return undefined;
}

const DATE_PARAM_PATTERN = /^\d{4}-\d{2}-\d{2}$/;

/** Validates a `YYYY-MM-DD` URL search param (date-range filters). */
export function toDateParam(value: string | undefined): string | undefined {
  return value && DATE_PARAM_PATTERN.test(value) ? value : undefined;
}

export const ticketsApi = {
  employee: {
    list: (params: TicketListParams) =>
      apiClient
        .get<PagedResult<TicketSummaryResponse>>("/api/tickets/employee", { params })
        .then((r) => r.data),
    get: (id: number) =>
      apiClient.get<TicketDetailResponse>(`/api/tickets/employee/${id}`).then((r) => r.data),
    /** Creates a ticket; AI analysis is queued automatically (201 + detail). */
    create: (body: { title: string; description: string }) =>
      apiClient.post<TicketDetailResponse>("/api/tickets/employee", body).then((r) => r.data),
  },
  agent: {
    list: (params: TicketListParams) =>
      apiClient.get<PagedResult<TicketSummaryResponse>>("/api/tickets/agent", { params }).then(
        (r) => r.data,
      ),
    get: (id: number) =>
      apiClient.get<TicketDetailResponse>(`/api/tickets/agent/${id}`).then((r) => r.data),
    /** Assigned → InProgress. */
    start: (id: number) =>
      apiClient.put<MessageResponse>(`/api/tickets/agent/${id}/start`).then((r) => r.data),
    /** InProgress → Resolved. */
    resolve: (id: number, resolution: string) =>
      apiClient
        .put<MessageResponse>(`/api/tickets/agent/${id}/resolve`, { resolution })
        .then((r) => r.data),
    /** Resolved → Closed. */
    close: (id: number) =>
      apiClient.put<MessageResponse>(`/api/tickets/agent/${id}/close`).then((r) => r.data),
    /** Assigned → AwaitingAssignment (hands the ticket back). */
    release: (id: number) =>
      apiClient.put<MessageResponse>(`/api/tickets/agent/${id}/release`).then((r) => r.data),
  },
  admin: {
    list: (params: TicketListParams) =>
      apiClient
        .get<PagedResult<TicketSummaryResponse>>("/api/admin/tickets", { params })
        .then((r) => r.data),
    get: (id: number) =>
      apiClient.get<TicketDetailResponse>(`/api/admin/tickets/${id}`).then((r) => r.data),
    /** Assign an agent (Created / AnalysisInProgress / AnalysisFailed / AwaitingAssignment). */
    assignAgent: (id: number, agentId: string) =>
      apiClient
        .put<MessageResponse>(`/api/admin/tickets/${id}/assign`, { agentId })
        .then((r) => r.data),
    /** Manually set category + priority (AnalysisFailed only; optional agent). */
    overrideAnalysis: (
      id: number,
      body: { categoryId: number; priorityId: number; agentId?: string },
    ) => apiClient.put<MessageResponse>(`/api/admin/tickets/${id}/override-analysis`, body).then((r) => r.data),
    /** Re-run the AI analysis (AnalysisFailed only). */
    retryAnalysis: (id: number) =>
      apiClient.post<MessageResponse>(`/api/admin/tickets/${id}/retry-analysis`).then((r) => r.data),
    /** Close any resolved ticket (Resolved only). */
    close: (id: number) =>
      apiClient.put<MessageResponse>(`/api/admin/tickets/${id}/close`).then((r) => r.data),
  },
  comments: {
    list: (ticketId: number) =>
      apiClient.get<CommentResponse[]>(`/api/tickets/${ticketId}/comments`).then((r) => r.data),
    create: (ticketId: number, content: string) =>
      apiClient
        .post<CommentResponse>(`/api/tickets/${ticketId}/comments`, { content })
        .then((r) => r.data),
  },
  history: {
    list: (ticketId: number) =>
      apiClient.get<TicketHistoryEvent[]>(`/api/tickets/${ticketId}/history`).then((r) => r.data),
  },
};
