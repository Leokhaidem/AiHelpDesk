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
const FILTER_KEYS = ["status", "categoryId", "priorityId", "fromDate", "toDate"];

/** P19 — an employee's own tickets, read-only. */
export function EmployeeTicketListPage() {
  const navigate = useNavigate();

  const [status, setStatus] = useUrlParam("status");
  const [categoryId, setCategoryId] = useUrlParam("categoryId");
  const [priorityId, setPriorityId] = useUrlParam("priorityId");
  const [fromDate, setFromDate] = useUrlParam("fromDate");
  const [toDate, setToDate] = useUrlParam("toDate");
  const [page, setPage] = usePageParam();
  const [pageSizeParam, setPageSizeParam] = useUrlParam("pageSize");
  const clearFilters = useClearParams();

  const pageSize = toPositiveInt(pageSizeParam) ?? DEFAULT_PAGE_SIZE;

  const query = useTicketList("employee", {
    page,
    pageSize,
    status: toTicketStatus(status),
    categoryId: toPositiveInt(categoryId),
    priorityId: toPositiveInt(priorityId),
    fromDate: toDateParam(fromDate),
    toDate: toDateParam(toDate),
  });

  const result = query.data;
  const hasActiveFilters = Boolean(status || categoryId || priorityId || fromDate || toDate);

  return (
    <div>
      <PageHeader title="My tickets" description="Tickets you have submitted." />

      <TicketFilterBar
        scope="employee"
        status={status}
        categoryId={categoryId}
        priorityId={priorityId}
        fromDate={fromDate}
        toDate={toDate}
        onStatusChange={setStatus}
        onCategoryIdChange={setCategoryId}
        onPriorityIdChange={setPriorityId}
        onFromDateChange={setFromDate}
        onToDateChange={setToDate}
        hasActiveFilters={hasActiveFilters}
        onClearFilters={() => clearFilters(FILTER_KEYS)}
      />

      {query.isError ? (
        <ErrorState error={query.error} onRetry={() => void query.refetch()} />
      ) : (
        <TicketTable
          scope="employee"
          data={result?.items ?? []}
          loading={query.isPending}
          page={page}
          pageSize={pageSize}
          totalCount={result?.totalCount ?? 0}
          onPageChange={setPage}
          onPageSizeChange={(size) => setPageSizeParam(String(size))}
          onRowClick={(ticket) => navigate(`/employee/tickets/${ticket.id}`)}
          empty={
            <EmptyState
              icon={<TicketX className="size-10" aria-hidden="true" />}
              title={hasActiveFilters ? "No tickets match your filters" : "No tickets yet"}
              description={
                hasActiveFilters
                  ? "Try adjusting or clearing the filters."
                  : "Submit your first ticket to get help from the AI help desk."
              }
            />
          }
        />
      )}
    </div>
  );
}
