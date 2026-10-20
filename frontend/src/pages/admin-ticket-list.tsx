import { useNavigate } from "react-router";
import { TicketX } from "lucide-react";

import { EmptyState } from "@/components/shared/empty-state";
import { ErrorState } from "@/components/shared/error-state";
import { PageHeader } from "@/components/shared/page-header";
import { toDateParam, toTicketStatus } from "@/features/tickets/api";
import { TicketFilterBar } from "@/features/tickets/components/ticket-filter-bar";
import { TicketTable } from "@/features/tickets/components/ticket-table";
import { useTicketList } from "@/features/tickets/hooks";
import { toPositiveInt, useClearParams, usePageParam, useUrlParam } from "@/lib/url-filters";

const DEFAULT_PAGE_SIZE = 10;
const FILTER_KEYS = [
  "status",
  "categoryId",
  "priorityId",
  "assignedAgentId",
  "agentInactive",
  "fromDate",
  "toDate",
];

/** P5 — every ticket in the system. */
export function AdminTicketListPage() {
  const navigate = useNavigate();

  const [status, setStatus] = useUrlParam("status");
  const [categoryId, setCategoryId] = useUrlParam("categoryId");
  const [priorityId, setPriorityId] = useUrlParam("priorityId");
  const [assignedAgentId, setAssignedAgentId] = useUrlParam("assignedAgentId");
  const [agentInactiveParam, setAgentInactiveParam] = useUrlParam("agentInactive");
  const [fromDate, setFromDate] = useUrlParam("fromDate");
  const [toDate, setToDate] = useUrlParam("toDate");
  const [page, setPage] = usePageParam();
  const [pageSizeParam, setPageSizeParam] = useUrlParam("pageSize");
  const clearFilters = useClearParams();

  const pageSize = toPositiveInt(pageSizeParam) ?? DEFAULT_PAGE_SIZE;
  const agentInactive = agentInactiveParam === "true";

  const query = useTicketList("admin", {
    page,
    pageSize,
    status: toTicketStatus(status),
    categoryId: toPositiveInt(categoryId),
    priorityId: toPositiveInt(priorityId),
    assignedAgentId,
    agentInactive,
    fromDate: toDateParam(fromDate),
    toDate: toDateParam(toDate),
  });

  const result = query.data;
  const hasActiveFilters = Boolean(
    status || categoryId || priorityId || assignedAgentId || agentInactive || fromDate || toDate,
  );

  return (
    <div>
      <PageHeader title="All tickets" description="Every ticket in the system." />

      <TicketFilterBar
        scope="admin"
        status={status}
        categoryId={categoryId}
        priorityId={priorityId}
        assignedAgentId={assignedAgentId}
        agentInactive={agentInactive}
        fromDate={fromDate}
        toDate={toDate}
        onStatusChange={setStatus}
        onCategoryIdChange={setCategoryId}
        onPriorityIdChange={setPriorityId}
        onAssignedAgentIdChange={setAssignedAgentId}
        onAgentInactiveChange={(value) => setAgentInactiveParam(value ? "true" : undefined)}
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
