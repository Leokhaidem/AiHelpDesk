import { useQuery } from "@tanstack/react-query";

import { queryKeys } from "@/lib/query-client";

import { dashboardApi } from "./api";

/** Dashboard refresh cadence (integration §10.12: 30 s). */
const REFETCH_INTERVAL = 30_000;

/**
 * P4 — service-wide (admin) / agent-scoped metrics with 30 s auto-refresh.
 * The query key is param-less (["dashboard","metrics"]): role scoping is
 * resolved server-side from the JWT, so a single shared key is correct.
 */
export function useDashboard() {
  return useQuery({
    queryKey: queryKeys.dashboard.metrics,
    queryFn: dashboardApi.get,
    refetchInterval: REFETCH_INTERVAL,
  });
}
