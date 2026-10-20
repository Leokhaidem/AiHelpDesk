import { useNavigate } from "react-router";
import { TicketX } from "lucide-react";

import { EmptyState } from "@/components/shared/empty-state";
import { ErrorState } from "@/components/shared/error-state";
import { PageHeader } from "@/components/shared/page-header";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { toDateParam, toTicketStatus } from "@/features/tickets/api";
import { TicketFilterBar } from "@/features/tickets/components/ticket-filter-bar";
import { TicketTable } from "@/features/tickets/components/ticket-table";
import { useTicketList } from "@/features/tickets/hooks";
import { toPositiveInt, useClearParams, usePageParam, useUrlParam } from "@/lib/url-filters";

const DEFAULT_PAGE_SIZE = 10;
const FILTER_KEYS = ["status", "categoryId", "priorityId", "fromDate", "toDate"];

/**
 * Tabs are synced with the `status` filter. The API accepts a single status
 * value, so an "Open" (Assigned + InProgress) tab is not expressible —
 * the All tab plus the status dropdown below covers those states.
 */
const AGENT_TABS = [
  { value: "all", label: "All" },
  { value: "Resolved", label: "Resolved" },
  { value: "Closed", label: "Closed" },
] as const;

/** P14 — an agent's assigned tickets with status tabs and lifecycle actions. */
export function AgentTicketListPage() {
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
  const activeTab = status === "Resolved" || status === "Closed" ? status : "all";

  const query = useTicketList("agent", {
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
      <PageHeader title="My tickets" description="Tickets assigned to you." />

      <Tabs
        value={activeTab}
        onValueChange={(value) => setStatus(value === "all" ? undefined : value)}
        className="mb-4"
      >
        <TabsList>
          {AGENT_TABS.map((tab) => (
            <TabsTrigger key={tab.value} value={tab.value}>
              {tab.label}
            </TabsTrigger>
          ))}
        </TabsList>
      </Tabs>

      <TicketFilterBar
        scope="agent"
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
          scope="agent"
          data={result?.items ?? []}
          loading={query.isPending}
          page={page}
          pageSize={pageSize}
          totalCount={result?.totalCount ?? 0}
          onPageChange={setPage}
          onPageSizeChange={(size) => setPageSizeParam(String(size))}
          onRowClick={(ticket) => navigate(`/agent/tickets/${ticket.id}`)}
          empty={
            <EmptyState
              icon={<TicketX className="size-10" aria-hidden="true" />}
              title={hasActiveFilters ? "No tickets match your filters" : "No assigned tickets"}
              description={
                hasActiveFilters
                  ? "Try adjusting or clearing the filters."
                  : "Tickets assigned to you will appear here."
              }
            />
          }
        />
      )}
    </div>
  );
}
