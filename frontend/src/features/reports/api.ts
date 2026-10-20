import { apiClient } from "@/lib/api-client";

import type { TicketSummaryResponse } from "@/features/tickets/api";
import type { PagedResult } from "@/types/api";

/** GET /api/reports query (integration §7 ReportQuery, §10.8). */
export type ReportQuery = {
  page?: number;
  pageSize?: number;
  status?: string;
  categoryId?: number;
  priorityId?: number;
  agentId?: string;
  fromDate?: string;
  toDate?: string;
};

export const reportsApi = {
  list: (params: ReportQuery) =>
    apiClient
      .get<PagedResult<TicketSummaryResponse>>("/api/reports", { params })
      .then((response) => response.data),
};
