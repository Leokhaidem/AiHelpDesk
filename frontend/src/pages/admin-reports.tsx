import { useNavigate } from "react-router";
import { TicketX } from "lucide-react";

import { EmptyState } from "@/components/shared/empty-state";
import { ErrorState } from "@/components/shared/error-state";
import { PageHeader } from "@/components/shared/page-header";
import { useReports } from "@/features/reports/hooks";
import { toDateParam, toTicketStatus } from "@/features/tickets/api";
import { TicketFilterBar } from "@/features/tickets/components/ticket-filter-bar";
import { TicketTable } from "@/features/tickets/components/ticket-table";
import { toPositiveInt, useClearParams, usePageParam, useUrlParam } from "@/lib/url-filters";

const DEFAULT_PAGE_SIZE = 50;
const FILTER_KEYS = ["status", "categoryId", "priorityId", "agentId", "fromDate", "toDate"];

/** P12 — ticket reports: date range + status + category + priority + agent. */
export function AdminReportsPage() {
  const navigate = useNavigate();

  const [status, setStatus] = useUrlParam("status");
  const [categoryId, setCategoryId] = useUrlParam("categoryId");
  const [priorityId, setPriorityId] = useUrlParam("priorityId");
  const [agentId, setAgentId] = useUrlParam("agentId");
  const [fromDate, setFromDate] = useUrlParam("fromDate");
  const [toDate, setToDate] = useUrlParam("toDate");
  const [page, setPage] = usePageParam();
  const [pageSizeParam, setPageSizeParam] = useUrlParam("pageSize");
  const clearFilters = useClearParams();

  const pageSize = toPositiveInt(pageSizeParam) ?? DEFAULT_PAGE_SIZE;

  const query = useReports({
    page,
    pageSize,
    status: toTicketStatus(status),
    categoryId: toPositiveInt(categoryId),
    priorityId: toPositiveInt(priorityId),
    agentId,
    fromDate: toDateParam(fromDate),
    toDate: toDateParam(toDate),
  });

  const result = query.data;
  const hasActiveFilters = Boolean(
    status || categoryId || priorityId || agentId || fromDate || toDate,
  );

  return (
    <div>
      <PageHeader
        title="Reports"
        description="Filtered ticket report across the whole service desk."
      />

      <TicketFilterBar
        scope="admin"
        status={status}
        categoryId={categoryId}
        priorityId={priorityId}
        assignedAgentId={agentId}
        fromDate={fromDate}
        toDate={toDate}
        onStatusChange={setStatus}
        onCategoryIdChange={setCategoryId}
        onPriorityIdChange={setPriorityId}
        onAssignedAgentIdChange={setAgentId}
        onFromDateChange={setFromDate}
        onToDateChange={setToDate}
        hasActiveFilters={hasActiveFilters}
        onClearFilters={() => clearFilters(FILTER_KEYS)}
      />

      {query.isError ? (
        <ErrorState error={query.error} onRetry={() => void query.refetch()} />
      ) : (
        <TicketTable
          scope="admin"
          data={result?.items ?? []}
          loading={query.isPending}
          page={page}
          pageSize={pageSize}
          totalCount={result?.totalCount ?? 0}
          onPageChange={setPage}
          onPageSizeChange={(size) => setPageSizeParam(String(size))}
          onRowClick={(ticket) => navigate(`/admin/tickets/${ticket.id}`)}
          empty={
            <EmptyState
              icon={<TicketX className="size-10" aria-hidden="true" />}
              title={hasActiveFilters ? "No tickets match your filters" : "No tickets yet"}
              description={
                hasActiveFilters
                  ? "Try adjusting or clearing the filters."
                  : "Tickets submitted by employees will appear here."
              }
            />
          }
        />
      )}
    </div>
  );
}