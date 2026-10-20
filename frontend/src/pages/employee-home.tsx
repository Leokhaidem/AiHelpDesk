import { Link, useNavigate } from "react-router";
import { ArrowRight, FileText, Plus, UserCheck } from "lucide-react";

import { EmptyState } from "@/components/shared/empty-state";
import { ErrorState } from "@/components/shared/error-state";
import { PageHeader } from "@/components/shared/page-header";
import { StatusBadge } from "@/components/shared/status-badge";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import type { AgentApplicationResponse, ApplicationStatus } from "@/features/applications/api";
import { useMyApplication } from "@/features/applications/hooks";
import { useTicketList } from "@/features/tickets/hooks";
import { formatDateTime } from "@/lib/dates";

const APPLICATION_TONES: Record<ApplicationStatus, string> = {
  Pending:
    "border-amber-300/60 bg-amber-50 text-amber-800 dark:border-amber-500/30 dark:bg-amber-500/10 dark:text-amber-300",
  Approved:
    "border-emerald-300/60 bg-emerald-50 text-emerald-800 dark:border-emerald-500/30 dark:bg-emerald-500/10 dark:text-emerald-300",
  Rejected: "border-destructive/30 bg-destructive/5 text-destructive",
  Withdrawn: "border-border bg-background text-muted-foreground",
};

function applicationText(data: AgentApplicationResponse): string {
  switch (data.status) {
    case "Pending":
      return `Submitted ${formatDateTime(data.createdAt)} — an admin will review it shortly.`;
    case "Approved":
      return "You have the Agent role — sign in again to use the agent area.";
    case "Rejected":
      return data.reviewNote ?? "An admin declined this application. You can apply again.";
    case "Withdrawn":
      return "You withdrew this application. You can apply again.";
  }
}

/** P17 — employee home: recent tickets, new-ticket CTA, application chip. */
export function EmployeeHomePage() {
  const navigate = useNavigate();
  const recent = useTicketList("employee", { page: 1, pageSize: 5 });
  const application = useMyApplication();

  const tickets = recent.data?.items ?? [];

  return (
    <div>
      <PageHeader
        title="Home"
        description="Your tickets and support activity."
        actions={
          <Button onClick={() => navigate("/employee/tickets/new")}>
            <Plus aria-hidden="true" />
            New ticket
          </Button>
        }
      />

      <div className="grid items-start gap-4 lg:grid-cols-[2fr_1fr]">
        <section className="rounded-lg border bg-card p-4">
          <div className="mb-2 flex items-center justify-between">
            <h2 className="text-sm font-medium text-muted-foreground">Recent tickets</h2>
            <Link
              to="/employee/tickets"
              className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
            >
              View all
              <ArrowRight className="size-3.5" aria-hidden="true" />
            </Link>
          </div>
          {recent.isError ? (
            <ErrorState
              error={recent.error}
              onRetry={() => void recent.refetch()}
              className="py-10"
            />
          ) : recent.isPending ? (
            <div className="space-y-2" aria-busy="true" aria-label="Loading recent tickets">
              <Skeleton className="h-10 w-full" />
              <Skeleton className="h-10 w-full" />
              <Skeleton className="h-10 w-full" />
            </div>
          ) : tickets.length === 0 ? (
            <EmptyState
              icon={<FileText className="size-10" aria-hidden="true" />}
              title="No tickets yet"
              description="Submit your first ticket to get help from the AI help desk."
              action={
                <Button size="sm" onClick={() => navigate("/employee/tickets/new")}>
                  <Plus aria-hidden="true" />
                  New ticket
                </Button>
              }
              className="py-10"
            />
          ) : (
            <ul className="divide-y">
              {tickets.map((ticket) => (
                <li key={ticket.id}>
                  <button
                    type="button"
                    onClick={() => navigate(`/employee/tickets/${ticket.id}`)}
                    className="flex w-full items-center justify-between gap-3 rounded-md px-2 py-2.5 text-left hover:bg-muted/40"
                  >
                    <span className="min-w-0">
                      <span className="block truncate text-sm font-medium">
                        #{ticket.id} · {ticket.title}
                      </span>
                      <span className="mt-0.5 block text-xs text-muted-foreground">
                        Updated {formatDateTime(ticket.updatedAt)}
                      </span>
                    </span>
                    <StatusBadge status={ticket.status} />
                  </button>
                </li>
              ))}
            </ul>
          )}
        </section>

        <section className="rounded-lg border bg-card p-4">
          <h2 className="mb-3 text-sm font-medium text-muted-foreground">Agent application</h2>
          {application.isPending ? (
            <Skeleton className="h-20 w-full" />
          ) : application.isError ? (
            <p className="text-sm text-muted-foreground">
              Couldn't load your application status right now.
            </p>
          ) : application.data === null ? (
            <div className="rounded-md bg-muted/40 p-3">
              <p className="text-sm font-medium">Not applied yet</p>
              <p className="mt-0.5 text-xs text-muted-foreground">
                Interested in helping as a support agent? Apply and an admin will review it.
              </p>
            </div>
          ) : (
            <div className="rounded-md bg-muted/40 p-3">
              <Badge
                variant="secondary"
                className={APPLICATION_TONES[application.data.status]}
              >
                {application.data.status}
              </Badge>
              <p className="mt-2 text-xs text-muted-foreground">
                {applicationText(application.data)}
              </p>
            </div>
          )}
          <div className="mt-4">
            <Button asChild variant="outline" size="sm">
              <Link to="/employee/agent-application">
                <UserCheck aria-hidden="true" />
                {application.data ? "View application" : "Apply as agent"}
              </Link>
            </Button>
          </div>
        </section>
      </div>
    </div>
  );
}
