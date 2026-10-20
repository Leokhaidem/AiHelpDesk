import { Alert, AlertDescription } from "@/components/ui/alert";
import { Skeleton } from "@/components/ui/skeleton";
import { formatDateTime } from "@/lib/dates";
import { isApiError } from "@/lib/error-mapping";

import { TICKET_EVENT_TYPE_LABELS } from "../api";
import { useTicketHistory } from "../hooks";

/** P6 — admin audit timeline (GET /api/tickets/{id}/history, MongoDB). */
export function TicketHistoryList({ ticketId }: { ticketId: number }) {
  const history = useTicketHistory(ticketId);

  return (
    <section aria-label="Ticket history" className="rounded-lg border bg-card p-4">
      <h2 className="mb-3 text-sm font-medium text-muted-foreground">History</h2>
      {history.isError ? (
        <Alert variant="destructive">
          <AlertDescription>
            {isApiError(history.error) ? history.error.detail : "History could not be loaded."}
          </AlertDescription>
        </Alert>
      ) : history.isPending ? (
        <div className="space-y-2" aria-busy="true" aria-label="Loading history">
          <Skeleton className="h-9 w-full" />
          <Skeleton className="h-9 w-full" />
          <Skeleton className="h-9 w-3/4" />
        </div>
      ) : history.data.length === 0 ? (
        <p className="text-sm text-muted-foreground">No history recorded yet.</p>
      ) : (
        <ol>
          {history.data.map((event) => (
            <li
              key={event.id}
              className="relative border-l-2 border-muted pb-4 pl-4 last:pb-0 last:border-transparent"
            >
              <span
                className="absolute top-1.5 -left-[5px] size-2 rounded-full bg-muted-foreground/40"
                aria-hidden="true"
              />
              <div className="flex flex-wrap items-baseline gap-x-2 text-sm">
                <span className="font-medium">{TICKET_EVENT_TYPE_LABELS[event.eventType]}</span>
                <span className="text-xs text-muted-foreground tabular-nums">
                  {formatDateTime(event.timestamp)}
                </span>
              </div>
              {event.oldValue || event.newValue ? (
                <p className="mt-0.5 text-xs text-muted-foreground">
                  {event.oldValue} <span aria-hidden="true">→</span> {event.newValue}
                </p>
              ) : null}
              {event.details ? (
                <p className="mt-1 text-sm whitespace-pre-wrap text-muted-foreground">{event.details}</p>
              ) : null}
            </li>
          ))}
        </ol>
      )}
    </section>
  );
}
