import type { ReactNode } from "react";
import { Link } from "react-router";
import { ArrowRight, FolderOpen, ListOrdered } from "lucide-react";

import { ErrorState } from "@/components/shared/error-state";
import { PageHeader } from "@/components/shared/page-header";
import { Skeleton } from "@/components/ui/skeleton";
import { useDashboard } from "@/features/dashboard/hooks";
import type { DashboardMetrics } from "@/features/dashboard/api";

type StatCardDef = { label: string; value: number; tone?: "danger" | "warning" };

function statCards(metrics: DashboardMetrics): StatCardDef[] {
  return [
    { label: "Total tickets", value: metrics.totalTickets },
    { label: "Created", value: metrics.created },
    { label: "Analysis in progress", value: metrics.analysisInProgress },
    { label: "Analysis failed", value: metrics.analysisFailed, tone: "danger" },
    { label: "Awaiting assignment", value: metrics.awaitingAssignment, tone: "warning" },
    { label: "Assigned", value: metrics.assigned },
    { label: "In progress", value: metrics.inProgress },
    { label: "Resolved", value: metrics.resolved },
    { label: "Closed", value: metrics.closed },
    { label: "Unassigned", value: metrics.unassigned },
  ];
}

function BarList({
  title,
  icon,
  items,
}: {
  title: string;
  icon: ReactNode;
  items: { name: string; count: number }[];
}) {
  const max = Math.max(1, ...items.map((item) => item.count));

  return (
    <section className="rounded-lg border bg-card p-4">
      <h2 className="mb-3 flex items-center gap-1.5 text-sm font-medium text-muted-foreground">
        {icon}
        {title}
      </h2>
      {items.length === 0 ? (
        <p className="text-sm text-muted-foreground">No tickets yet.</p>
      ) : (
        <ul className="space-y-2.5">
          {items.map((item) => (
            <li key={item.name}>
              <div className="flex items-baseline justify-between gap-4 text-sm">
                <span className="truncate">{item.name}</span>
                <span className="text-muted-foreground tabular-nums">{item.count}</span>
              </div>
              <div className="mt-1 h-1.5 overflow-hidden rounded-full bg-muted">
                <div
                  className="h-full rounded-full bg-primary/70"
                  style={{ width: `${(item.count / max) * 100}%` }}
                />
              </div>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}

/** P4 — admin dashboard: service-wide stats with 30 s auto-refresh. */
export function AdminHomePage() {
  const query = useDashboard();

  return (
    <div>
      <PageHeader
        title="Dashboard"
        description="Help desk overview — refreshes automatically every 30 seconds."
      />
      {query.isError ? (
        <ErrorState error={query.error} onRetry={() => void query.refetch()} />
      ) : query.isPending ? (
        <div className="space-y-4" aria-busy="true" aria-label="Loading dashboard">
          <div className="grid gap-3 sm:grid-cols-2 md:grid-cols-3 xl:grid-cols-5">
            {Array.from({ length: 10 }, (_, index) => (
              <Skeleton key={index} className="h-20 w-full" />
            ))}
          </div>
          <div className="grid items-start gap-4 lg:grid-cols-2">
            <Skeleton className="h-56 w-full" />
            <Skeleton className="h-56 w-full" />
          </div>
        </div>
      ) : (
        <>
          <div className="grid gap-3 sm:grid-cols-2 md:grid-cols-3 xl:grid-cols-5">
            {statCards(query.data).map((card) => (
              <div key={card.label} className="rounded-lg border bg-card p-4">
                <p className="text-xs text-muted-foreground">{card.label}</p>
                <p
                  className={
                    "mt-1 text-2xl font-semibold tabular-nums " +
                    (card.tone === "danger" && card.value > 0
                      ? "text-destructive"
                      : card.tone === "warning" && card.value > 0
                        ? "text-amber-600 dark:text-amber-400"
                        : "")
                  }
                >
                  {card.value}
                </p>
              </div>
            ))}
          </div>

          <div className="mt-4 grid gap-3 md:grid-cols-2">
            <Link
              to="/admin/tickets?status=AnalysisFailed"
              className="group flex items-center justify-between gap-4 rounded-lg border bg-card p-4 transition-colors hover:bg-muted/40"
            >
              <div>
                <p className="text-sm font-medium">Failed analyses</p>
                <p className="mt-0.5 text-xs text-muted-foreground">
                  {query.data.analysisFailed} ticket
                  {query.data.analysisFailed === 1 ? "" : "s"} need a retry or manual override.
                </p>
              </div>
              <ArrowRight
                className="size-4 shrink-0 text-muted-foreground transition-transform group-hover:translate-x-0.5"
                aria-hidden="true"
              />
            </Link>
            <Link
              to="/admin/tickets?status=AwaitingAssignment"
              className="group flex items-center justify-between gap-4 rounded-lg border bg-card p-4 transition-colors hover:bg-muted/40"
            >
              <div>
                <p className="text-sm font-medium">Awaiting assignment</p>
                <p className="mt-0.5 text-xs text-muted-foreground">
                  {query.data.awaitingAssignment} ticket
                  {query.data.awaitingAssignment === 1 ? "" : "s"} need an agent.
                </p>
              </div>
              <ArrowRight
                className="size-4 shrink-0 text-muted-foreground transition-transform group-hover:translate-x-0.5"
                aria-hidden="true"
              />
            </Link>
          </div>

          <div className="mt-4 grid items-start gap-4 lg:grid-cols-2">
            <BarList
              title="Tickets by category"
              icon={<FolderOpen className="size-4" aria-hidden="true" />}
              items={query.data.byCategory.map((item) => ({
                name: item.categoryName,
                count: item.count,
              }))}
            />
            <BarList
              title="Tickets by priority"
              icon={<ListOrdered className="size-4" aria-hidden="true" />}
              items={query.data.byPriority.map((item) => ({
                name: item.priorityName,
                count: item.count,
              }))}
            />
          </div>
        </>
      )}
    </div>
  );
}
