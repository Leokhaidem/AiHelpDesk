import { useEffect, useState } from "react";
import { Loader2, Play, RotateCcw, SquareCheck, XCircle } from "lucide-react";

import { ConfirmDialog } from "@/components/shared/confirm-dialog";
import { FieldError } from "@/components/shared/field-error";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { isApiError } from "@/lib/error-mapping";
import { TICKET_STATUS_LABELS } from "@/types/api";

import { useCloseTicket, useReleaseTicket, useResolveTicket, useStartTicket } from "../hooks";

import type { TicketDetailResponse } from "../api";

const RESOLUTION_MIN = 10;
const RESOLUTION_MAX = 5000;

/**
 * Role-aware action toolbar for an agent's ticket (P15, plan §8). Buttons
 * follow the backend state machine: start (Assigned), resolve (InProgress),
 * close (Resolved), release (Assigned). A 409 "business rule" rejection is
 * shown inline so the agent sees exactly which transition was refused.
 */
export function AgentTicketActions({ ticket }: { ticket: TicketDetailResponse }) {
  const [resolveOpen, setResolveOpen] = useState(false);
  const [releaseOpen, setReleaseOpen] = useState(false);
  const [resolution, setResolution] = useState("");
  const [resolutionTouched, setResolutionTouched] = useState(false);
  const [conflictDismissed, setConflictDismissed] = useState(false);

  const start = useStartTicket();
  const resolve = useResolveTicket();
  const close = useCloseTicket();
  const release = useReleaseTicket();

  const status = ticket.status;
  const canStart = status === "Assigned";
  const canResolve = status === "InProgress";
  const canClose = status === "Resolved";
  const canRelease = status === "Assigned";

  const anyPending = start.isPending || resolve.isPending || close.isPending || release.isPending;

  useEffect(() => {
    if (anyPending) {
      setConflictDismissed(false);
    }
  }, [anyPending]);

  const conflict = [start, resolve, close, release].find(
    (m) => isApiError(m.error) && m.error.status === 409,
  );

  if (!canStart && !canResolve && !canClose && !canRelease) {
    if (status === "Closed") {
      return null;
    }
    return (
      <p className="text-sm text-muted-foreground">
        No actions available — this ticket is currently {TICKET_STATUS_LABELS[status].toLowerCase()}.
      </p>
    );
  }

  const resolutionError =
    resolution.trim().length < RESOLUTION_MIN
      ? `Resolution must be at least ${RESOLUTION_MIN} characters.`
      : resolution.length > RESOLUTION_MAX
        ? `Resolution must not exceed ${RESOLUTION_MAX} characters.`
        : undefined;

  const openResolveDialog = () => {
    setResolution("");
    setResolutionTouched(false);
    setResolveOpen(true);
  };

  return (
    <section className="rounded-lg border bg-card p-4">
      <div className="flex flex-wrap items-center gap-2">
        {canStart ? (
          <Button onClick={() => start.mutate(ticket.id)} disabled={anyPending}>
            {start.isPending ? (
              <Loader2 className="animate-spin" aria-hidden="true" />
            ) : (
              <Play aria-hidden="true" />
            )}
            Start work
          </Button>
        ) : null}
        {canResolve ? (
          <Button onClick={openResolveDialog} disabled={anyPending}>
            <SquareCheck aria-hidden="true" />
            Resolve ticket…
          </Button>
        ) : null}
        {canClose ? (
          <Button
            variant="destructive"
            onClick={() => close.mutate(ticket.id)}
            disabled={anyPending}
          >
            {close.isPending ? (
              <Loader2 className="animate-spin" aria-hidden="true" />
            ) : (
              <XCircle aria-hidden="true" />
            )}
            Close ticket
          </Button>
        ) : null}
        {canRelease ? (
          <Button variant="outline" onClick={() => setReleaseOpen(true)} disabled={anyPending}>
            <RotateCcw aria-hidden="true" />
            Release ticket
          </Button>
        ) : null}
      </div>

      {conflict && !conflictDismissed ? (
        <Alert variant="destructive" className="mt-3">
          <AlertTitle>
            {isApiError(conflict.error)
              ? conflict.error.detail
              : "This action is not allowed in the current status."}
          </AlertTitle>
          <AlertDescription>
            The ticket status may have changed, or this transition is not permitted. Dismiss to try
            another action.
          </AlertDescription>
          <Button
            variant="ghost"
            size="sm"
            className="mt-1 text-destructive"
            onClick={() => setConflictDismissed(true)}
          >
            Dismiss
          </Button>
        </Alert>
      ) : null}

      <Dialog open={resolveOpen} onOpenChange={(next) => !resolve.isPending && setResolveOpen(next)}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>Resolve ticket #{ticket.id}</DialogTitle>
            <DialogDescription>
              Record how you resolved this ticket. The employee will see this on their ticket page.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-3">
            <div>
              <Label htmlFor="resolve-text">Resolution</Label>
              <Textarea
                id="resolve-text"
                rows={6}
                className="mt-1.5"
                value={resolution}
                placeholder="What did you do to fix the issue?"
                aria-invalid={resolutionError ? true : undefined}
                onChange={(event) => setResolution(event.target.value)}
                onBlur={() => setResolutionTouched(true)}
              />
              <div className="mt-1 flex items-center justify-between gap-2">
                <FieldError message={resolutionTouched ? resolutionError : undefined} />
                <span className="ml-auto text-xs text-muted-foreground tabular-nums">
                  {resolution.length}/{RESOLUTION_MAX}
                </span>
              </div>
              {ticket.aiSuggestedResolution && resolution.trim().length === 0 ? (
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  className="text-muted-foreground"
                  onClick={() => setResolution(ticket.aiSuggestedResolution ?? "")}
                >
                  Use the AI suggested resolution
                </Button>
              ) : null}
            </div>
          </div>
          <DialogFooter>
            <Button variant="ghost" disabled={resolve.isPending} onClick={() => setResolveOpen(false)}>
              Cancel
            </Button>
            <Button
              disabled={resolve.isPending || Boolean(resolutionError)}
              onClick={() => {
                setResolutionTouched(true);
                if (!resolutionError) {
                  resolve.mutate(
                    { ticketId: ticket.id, resolution: resolution.trim() },
                    { onSettled: () => setResolveOpen(false) },
                  );
                }
              }}
            >
              {resolve.isPending ? <Loader2 className="animate-spin" aria-hidden="true" /> : null}
              Resolve
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <ConfirmDialog
        open={releaseOpen}
        onOpenChange={setReleaseOpen}
        title="Release this ticket?"
        description="The ticket goes back to the assignment queue and may be assigned to another agent."
        confirmLabel="Release ticket"
        destructive
        loading={release.isPending}
        onConfirm={() => {
          release.mutate(ticket.id, {
            onSettled: () => setReleaseOpen(false),
          });
        }}
      />
    </section>
  );
}