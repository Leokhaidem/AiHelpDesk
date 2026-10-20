import { Link, useParams } from "react-router";
import { ArrowLeft, TicketX } from "lucide-react";

import { EmptyState } from "@/components/shared/empty-state";
import { ErrorState } from "@/components/shared/error-state";
import { PageHeader } from "@/components/shared/page-header";
import { StatusBadge } from "@/components/shared/status-badge";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { CommentThread } from "@/features/tickets/components/comment-thread";
import { TicketDetailPanels } from "@/features/tickets/components/ticket-detail-panels";
import { useTicketDetail } from "@/features/tickets/hooks";
import { formatDateTime } from "@/lib/dates";
import { isApiError } from "@/lib/error-mapping";

/** P20 — an employee's own ticket, read-only + comments. Polls while the AI runs. */
export function EmployeeTicketDetailPage() {
  const { id } = useParams();
  const ticketId = Number.parseInt(id ?? "", 10);
  const valid = Number.isFinite(ticketId) && ticketId > 0;

  const query = useTicketDetail(ticketId, "employee", valid);

  if (!valid) {
    return (
      <div>
        <PageHeader title="Ticket" />
        <EmptyState
          title="Ticket not found"
          description="The ticket ID in the address bar is not valid."
        />
      </div>
    );
  }

  if (query.isPending) {
    return (
      <div>
        <PageHeader title="Ticket" />
        <div className="space-y-4" aria-busy="true" aria-label="Loading ticket">
          <Skeleton className="h-8 w-64" />
          <Skeleton className="h-40 w-full" />
          <Skeleton className="h-64 w-full" />
        </div>
      </div>
    );
  }

  if (query.isError || !query.data) {
    // A 404 means the ticket is missing or not owned by this employee.
    if (isApiError(query.error) && query.error.status === 404) {
      return (
        <div>
          <PageHeader title="Ticket" />
          <EmptyState
            icon={<TicketX className="size-10" aria-hidden="true" />}
            title="Ticket not found"
            description="This ticket does not exist or it is not one of your tickets."
            action={
              <Button asChild variant="outline">
                <Link to="/employee/tickets">Back to my tickets</Link>
              </Button>
            }
          />
        </div>
      );
    }
    return (
      <div>
        <PageHeader title="Ticket" />
        <ErrorState error={query.error} onRetry={() => void query.refetch()} />
      </div>
    );
  }

  const ticket = query.data;

  return (
    <div>
      <div className="mb-3">
        <Link
          to="/employee/tickets"
          className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
        >
          <ArrowLeft className="size-4" aria-hidden="true" />
          Back to my tickets
        </Link>
      </div>
      <PageHeader
        title={ticket.title}
        description={`Ticket #${ticket.id} · submitted ${formatDateTime(ticket.createdAt)}`}
        actions={<StatusBadge status={ticket.status} />}
      />
      <TicketDetailPanels ticket={ticket}>
        <CommentThread ticketId={ticket.id} />
      </TicketDetailPanels>
    </div>
  );
}
