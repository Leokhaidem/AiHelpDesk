/**
 * Shared wire types used by several features (docs/frontend-integration.md
 * section 7). Feature-specific DTOs live in each feature's api.ts.
 */

export type PagedResult<T> = {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
};

/** Ticket status — PascalCase string in the ticket DTOs (integration §7). */
export type TicketStatus =
  | "Created"
  | "AnalysisInProgress"
  | "Assigned"
  | "InProgress"
  | "Resolved"
  | "Closed"
  | "AnalysisFailed"
  | "AwaitingAssignment";

export const TICKET_STATUSES: readonly TicketStatus[] = [
  "Created",
  "AnalysisInProgress",
  "Assigned",
  "InProgress",
  "Resolved",
  "Closed",
  "AnalysisFailed",
  "AwaitingAssignment",
];

/** Human-readable labels for badges and filter options. */
export const TICKET_STATUS_LABELS: Record<TicketStatus, string> = {
  Created: "Created",
  AnalysisInProgress: "Analysis in progress",
  Assigned: "Assigned",
  InProgress: "In progress",
  Resolved: "Resolved",
  Closed: "Closed",
  AnalysisFailed: "Analysis failed",
  AwaitingAssignment: "Awaiting assignment",
};

/** Ticket history event type — numeric on the wire (integration §7). */
export type TicketEventType = 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11;

/**
 * Body of the admin ticket operations and the agent start/resolve/close/
 * release endpoints — they return a confirmation message, not the ticket.
 */
export type MessageResponse = { message: string };
