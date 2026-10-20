import type { ReactNode } from "react";
import { createColumnHelper } from "@tanstack/react-table";

import { DataTable } from "@/components/shared/data-table";
import { PriorityBadge } from "@/components/shared/priority-badge";
import { StatusBadge } from "@/components/shared/status-badge";
import { formatDateTime } from "@/lib/dates";

import type { TicketScope } from "../hooks";
import type { TicketSummaryResponse } from "../api";

const columnHelper = createColumnHelper<TicketSummaryResponse>();

const DASH = "—";

const baseColumns = [
  columnHelper.accessor("id", {
    header: "ID",
    cell: (info) => <span className="text-muted-foreground tabular-nums">{info.getValue()}</span>,
  }),
  columnHelper.accessor("title", {
    header: "Title",
    cell: (info) => <span className="font-medium">{info.getValue()}</span>,
  }),
  columnHelper.accessor("categoryName", {
    header: "Category",
    cell: (info) => info.getValue() ?? DASH,
  }),
  columnHelper.accessor("priorityName", {
    header: "Priority",
    cell: (info) => <PriorityBadge name={info.getValue()} />,
  }),
  columnHelper.accessor("status", {
    header: "Status",
    cell: (info) => <StatusBadge status={info.getValue()} />,
  }),
  columnHelper.accessor("assignedAgentName", {
    id: "agent",
    header: "Agent",
    cell: (info) => info.getValue() ?? DASH,
  }),
  columnHelper.accessor("createdAt", {
    header: "Created",
    cell: (info) => formatDateTime(info.getValue()),
  }),
  columnHelper.accessor("updatedAt", {
    id: "updated",
    header: "Updated",
    cell: (info) => formatDateTime(info.getValue()),
  }),
];

/** Column sets per role (plan §3): employee has no agent/updated columns. */
function columnsForScope(scope: TicketScope) {
  if (scope === "admin") {
    return baseColumns;
  }
  if (scope === "agent") {
    return baseColumns.filter((column) => column.id !== "updated");
  }
  return baseColumns.filter((column) => column.id !== "agent" && column.id !== "updated");
}

type TicketTableProps = {
  scope: TicketScope;
  data: TicketSummaryResponse[];
  loading: boolean;
  page: number;
  pageSize: number;
  totalCount: number;
  onPageChange: (page: number) => void;
  onPageSizeChange: (pageSize: number) => void;
  onRowClick: (ticket: TicketSummaryResponse) => void;
  empty?: ReactNode;
};

/** Role-aware ticket list table (plan §8). Row click navigates to detail. */
export function TicketTable({
  scope,
  data,
  loading,
  page,
  pageSize,
  totalCount,
  onPageChange,
  onPageSizeChange,
  onRowClick,
  empty,
}: TicketTableProps) {
  return (
    <DataTable
      columns={columnsForScope(scope)}
      data={data}
      loading={loading}
      page={page}
      pageSize={pageSize}
      totalCount={totalCount}
      onPageChange={onPageChange}
      onPageSizeChange={onPageSizeChange}
      onRowClick={onRowClick}
      empty={empty}
    />
  );
}
