import type { ReactNode } from "react";

import { AIBanner } from "@/components/shared/ai-banner";
import { formatDateTime } from "@/lib/dates";

import type { TicketDetailResponse } from "../api";

function MetaItem({ label, value }: { label: string; value: ReactNode }) {
  return (
    <div className="flex items-baseline justify-between gap-4 py-1.5">
      <dt className="shrink-0 text-muted-foreground">{label}</dt>
      <dd className="text-right font-medium break-words">{value}</dd>
    </div>
  );
}

type TicketDetailPanelsProps = {
  ticket: TicketDetailResponse;
  /** Role-specific actions rendered inside the AI failed state (admin, M7). */
  aiActions?: ReactNode;
  /** Rendered full-width below the grid — comments (M5), timeline (M7). */
  children?: ReactNode;
};

/**
 * Shared ticket detail layout (P6/P15/P20): description + AI panel +
 * resolution on the left, metadata on the right.
 */
export function TicketDetailPanels({ ticket, aiActions, children }: TicketDetailPanelsProps) {
  return (
    <div className="grid items-start gap-4 lg:grid-cols-[2fr_1fr]">
      <div className="space-y-4">
        <section className="rounded-lg border bg-card p-4">
          <h2 className="mb-2 text-sm font-medium text-muted-foreground">Description</h2>
          <p className="text-sm whitespace-pre-wrap">{ticket.description}</p>
        </section>
        <AIBanner
          status={ticket.status}
          aiSummary={ticket.aiSummary}
          aiSuggestedResolution={ticket.aiSuggestedResolution}
          actions={aiActions}
        />
        {ticket.resolution ? (
          <section className="rounded-lg border border-emerald-300/60 bg-emerald-50 p-4 dark:border-emerald-500/30 dark:bg-emerald-500/10">
            <h2 className="mb-2 text-sm font-medium text-emerald-800 dark:text-emerald-300">
              Resolution
            </h2>
            <p className="text-sm whitespace-pre-wrap">{ticket.resolution}</p>
          </section>
        ) : null}
      </div>
      <aside>
        <section className="rounded-lg border bg-card p-4">
          <h2 className="mb-2 text-sm font-medium text-muted-foreground">Details</h2>
          <dl className="divide-y">
            <MetaItem label="Category" value={ticket.categoryName ?? "—"} />
            <MetaItem label="Priority" value={ticket.priorityName ?? "—"} />
            <MetaItem label="Employee" value={ticket.employeeName ?? ticket.employeeId} />
            <MetaItem label="Assigned agent" value={ticket.assignedAgentName ?? "—"} />
            <MetaItem label="Created" value={formatDateTime(ticket.createdAt)} />
            <MetaItem label="Updated" value={formatDateTime(ticket.updatedAt)} />
            {ticket.resolvedAt ? (
              <MetaItem label="Resolved" value={formatDateTime(ticket.resolvedAt)} />
            ) : null}
            {ticket.closedAt ? (
              <MetaItem label="Closed" value={formatDateTime(ticket.closedAt)} />
            ) : null}
          </dl>
        </section>
      </aside>
      {children ? <div className="space-y-4 lg:col-span-2">{children}</div> : null}
    </div>
  );
}
