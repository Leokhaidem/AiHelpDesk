import { useQuery } from "@tanstack/react-query";

import { queryKeys } from "@/lib/query-client";

import { reportsApi, type ReportQuery } from "./api";

/** Same 30 s refresh cadence as the list views (query client §6). */
const REFETCH_INTERVAL = 30_000;

/** P12 — filtered ticket report, server-paged (queryKeys.reports.list). */
export function useReports(params: ReportQuery) {
  return useQuery({
    queryKey: queryKeys.reports.list(params),
    queryFn: () => reportsApi.list(params),
    refetchInterval: REFETCH_INTERVAL,
  });
}
