import { useEffect, useState } from "react";
import { Loader2, RefreshCcw, UserPlus, Wand2, XCircle } from "lucide-react";

import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { ConfirmDialog } from "@/components/shared/confirm-dialog";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { useCategories } from "@/features/categories/hooks";
import { usePriorities } from "@/features/priorities/hooks";
import { useActiveAgents } from "@/features/users/hooks";
import { isApiError } from "@/lib/error-mapping";

import type { TicketDetailResponse } from "../api";
import {
  useAdminCloseTicket,
  useAssignAgent,
  useOverrideAnalysis,
  useRetryAnalysis,
} from "../hooks";

const ASSIGNABLE_STATUSES = [
  "Created",
  "AnalysisInProgress",
  "AnalysisFailed",
  "AwaitingAssignment",
];
const OVERRIDABLE_STATUSES = ["AnalysisFailed", "AnalysisInProgress"];

/**
 * P6 — admin ticket ops toolbar: assign agent, override analysis (with
 * optional agent), retry analysis, close (terminal). 409 conflicts are
 * shown inline; the detail polling auto-refreshes the row.
 */
export function AdminTicketActions({ ticket }: { ticket: TicketDetailResponse }) {
  const [assignOpen, setAssignOpen] = useState(false);
  const [overrideOpen, setOverrideOpen] = useState(false);
  const [closeOpen, setCloseOpen] = useState(false);
  const [assignAgentId, setAssignAgentId] = useState("");
  const [categoryId, setCategoryId] = useState("");
  const [priorityId, setPriorityId] = useState("");
  const [overrideAgentId, setOverrideAgentId] = useState("");
  const [conflictDismissed, setConflictDismissed] = useState(false);

  const assign = useAssignAgent();
  const override = useOverrideAnalysis();
  const retry = useRetryAnalysis();
  const close = useAdminCloseTicket();

  const agents = useActiveAgents(assignOpen || overrideOpen);
  const categories = useCategories(true);
  const priorities = usePriorities(true);

  const canAssign = ASSIGNABLE_STATUSES.includes(ticket.status);
  const canOverride = OVERRIDABLE_STATUSES.includes(ticket.status);
  const canRetry = ticket.status === "AnalysisFailed";
  const canClose = ticket.status === "Resolved";
  const anyPending =
    assign.isPending || override.isPending || retry.isPending || close.isPending;

  const hasConflict = [assign, override, retry, close].some(
    (mutation) => isApiError(mutation.error) && mutation.error.status === 409,
  );

  useEffect(() => {
    if (!anyPending) setConflictDismissed(false);
  }, [anyPending]);

  const openAssign = () => {
    setAssignAgentId("");
    setAssignOpen(true);
  };

  const openOverride = () => {
    setCategoryId("");
    setPriorityId("");
    setOverrideAgentId("");
    setOverrideOpen(true);
  };

  const applyAssign = () => {
    if (!assignAgentId) return;
    assign.mutate(
      { ticketId: ticket.id, agentId: assignAgentId },
      { onSettled: () => setAssignOpen(false) },
    );
  };

  const applyOverride = () => {
    if (!categoryId || !priorityId) return;
    override.mutate(
      {
        ticketId: ticket.id,
        categoryId: Number(categoryId),
        priorityId: Number(priorityId),
        agentId: overrideAgentId || undefined,
      },
      { onSettled: () => setOverrideOpen(false) },
    );
  };
  if (!canAssign && !canOverride && !canRetry && !canClose) {
    return (
      <p className="text-sm text-muted-foreground">
        {ticket.status === "Closed"
          ? "This ticket is closed — no further actions are available."
          : "This ticket is in progress — no admin actions needed right now."}
      </p>
    );
  }

  return (
    <div className="space-y-3">
      {hasConflict && !conflictDismissed ? (
        <Alert variant="destructive">
          <AlertTitle>Out of date</AlertTitle>
          <AlertDescription>
            Someone else changed this ticket just now. The details have been refreshed — check the
            current status before trying again.
          </AlertDescription>
        </Alert>
      ) : null}
      <div className="flex flex-wrap items-center gap-2">
        {canAssign ? (
          <Button onClick={openAssign} disabled={anyPending}>
            {assign.isPending ? (
              <Loader2 className="animate-spin" aria-hidden="true" />
            ) : (
              <UserPlus className="size-4" aria-hidden="true" />
            )}
            Assign agent
          </Button>
        ) : null}
        {canOverride ? (
          <Button variant="outline" onClick={openOverride} disabled={anyPending}>
            <Wand2 className="size-4" aria-hidden="true" />
            Override analysis
          </Button>
        ) : null}
        {canRetry ? (
          <Button variant="outline" onClick={() => retry.mutate(ticket.id)} disabled={anyPending}>
            {retry.isPending ? (
              <Loader2 className="animate-spin" aria-hidden="true" />
            ) : (
              <RefreshCcw className="size-4" aria-hidden="true" />
            )}
            Retry analysis
          </Button>
        ) : null}
        {canClose ? (
          <Button variant="outline" onClick={() => setCloseOpen(true)} disabled={anyPending}>
            <XCircle className="size-4" aria-hidden="true" />
            Close ticket
          </Button>
        ) : null}
      </div>

      {/* Assign agent */}
      <Dialog
        open={assignOpen}
        onOpenChange={(next) => !assign.isPending && setAssignOpen(next)}
      >
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Assign an agent</DialogTitle>
            <DialogDescription>
              Ticket #{ticket.id} moves to the Assigned status with the selected agent.
            </DialogDescription>
          </DialogHeader>
          <div>
            <Label htmlFor="assign-agent">Agent</Label>
            <Select value={assignAgentId || undefined} onValueChange={setAssignAgentId}>
              <SelectTrigger id="assign-agent" className="mt-1.5">
                <SelectValue placeholder="Select an agent" />
              </SelectTrigger>
              <SelectContent>
                {(agents.data ?? []).map((agent) => (
                  <SelectItem key={agent.id} value={agent.id}>
                    {agent.fullName}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            {!assign.isPending && (agents.isError || (agents.data?.length ?? 0) === 0) ? (
              <p className="mt-1.5 text-xs text-muted-foreground">
                {agents.isError
                  ? "Active agents could not be loaded."
                  : "No active agents — enable one under Users first."}
              </p>
            ) : null}
          </div>
          <DialogFooter>
            <Button variant="ghost" disabled={assign.isPending} onClick={() => setAssignOpen(false)}>
              Cancel
            </Button>
            <Button disabled={assign.isPending || !assignAgentId} onClick={applyAssign}>
              {assign.isPending ? <Loader2 className="animate-spin" aria-hidden="true" /> : null}
              Assign
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
      {/* Override analysis */}
      <Dialog
        open={overrideOpen}
        onOpenChange={(next) => !override.isPending && setOverrideOpen(next)}
      >
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Override analysis</DialogTitle>
            <DialogDescription>
              Set category and priority manually. With an agent selected the ticket goes straight
              to Assigned; otherwise it waits for assignment.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-3">
            <div>
              <Label htmlFor="override-category">Category</Label>
              <Select value={categoryId || undefined} onValueChange={setCategoryId}>
                <SelectTrigger id="override-category" className="mt-1.5">
                  <SelectValue placeholder="Select a category" />
                </SelectTrigger>
                <SelectContent>
                  {(categories.data ?? []).map((category) => (
                    <SelectItem key={category.id} value={String(category.id)}>
                      {category.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div>
              <Label htmlFor="override-priority">Priority</Label>
              <Select value={priorityId || undefined} onValueChange={setPriorityId}>
                <SelectTrigger id="override-priority" className="mt-1.5">
                  <SelectValue placeholder="Select a priority" />
                </SelectTrigger>
                <SelectContent>
                  {(priorities.data ?? []).map((priority) => (
                    <SelectItem key={priority.id} value={String(priority.id)}>
                      {priority.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div>
              <Label htmlFor="override-agent">Agent (optional)</Label>
              <Select
                value={overrideAgentId || "unassigned"}
                onValueChange={(value) =>
                  setOverrideAgentId(value === "unassigned" ? "" : value)
                }
              >
                <SelectTrigger id="override-agent" className="mt-1.5">
                  <SelectValue placeholder="Leave unassigned" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="unassigned">Leave unassigned</SelectItem>
                  {(agents.data ?? []).map((agent) => (
                    <SelectItem key={agent.id} value={agent.id}>
                      {agent.fullName}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>
          <DialogFooter>
            <Button variant="ghost" disabled={override.isPending} onClick={() => setOverrideOpen(false)}>
              Cancel
            </Button>
            <Button disabled={override.isPending || !categoryId || !priorityId} onClick={applyOverride}>
              {override.isPending ? <Loader2 className="animate-spin" aria-hidden="true" /> : null}
              Apply override
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Close (terminal) */}
      <ConfirmDialog
        open={closeOpen}
        onOpenChange={setCloseOpen}
        title="Close this ticket?"
        description={`Closing is terminal — ticket #${ticket.id} cannot be reopened and the employee will see it as closed.`}
        confirmLabel="Close ticket"
        loading={close.isPending}
        onConfirm={() =>
          close.mutate(ticket.id, { onSettled: () => setCloseOpen(false) })
        }
      />
    </div>
  );
}
