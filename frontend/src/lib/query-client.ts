import { QueryClient } from "@tanstack/react-query";

import { isApiError } from "@/lib/error-mapping";

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      // Do not retry client errors — only transient failures (5xx, network).
      retry: (failureCount, error) => {
        if (isApiError(error) && error.status >= 400 && error.status < 500) {
          return false;
        }
        return failureCount < 2;
      },
      refetchOnWindowFocus: true,
      staleTime: 15_000,
    },
  },
});

/**
 * Query-key factories — the single source for cache keys and invalidation
 * (plan section 4.3).
 */
export const queryKeys = {
  tickets: {
    list: (scope: "admin" | "agent" | "employee", params: Record<string, unknown>) =>
      ["tickets", "list", scope, params] as const,
    detail: (scope: "admin" | "agent" | "employee", id: string) =>
      ["tickets", "detail", scope, id] as const,
    comments: (ticketId: string) => ["tickets", "comments", ticketId] as const,
    history: (ticketId: string) => ["tickets", "history", ticketId] as const,
  },
  categories: {
    all: (activeOnly: boolean) => ["categories", activeOnly] as const,
    public: ["categories", "public"] as const,
  },
  priorities: {
    all: (activeOnly: boolean) => ["priorities", activeOnly] as const,
    public: ["priorities", "public"] as const,
  },
  users: {
    all: (role?: string) => ["users", "all", role ?? "any"] as const,
    byId: (id: string) => ["users", "id", id] as const,
    categories: (id: string) => ["users", "categories", id] as const,
    agents: ["users", "agents"] as const,
  },
  applications: {
    adminList: (status: string, page: number, pageSize: number) =>
      ["applications", "admin", status, page, pageSize] as const,
    mine: ["applications", "mine"] as const,
  },
  profile: {
    me: ["agent", "me"] as const,
    categories: ["agent", "me", "categories"] as const,
  },
  dashboard: {
    metrics: ["dashboard", "metrics"] as const,
  },
  reports: {
    list: (params: Record<string, unknown>) => ["reports", "list", params] as const,
  },
} as const;
