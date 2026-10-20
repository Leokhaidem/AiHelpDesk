import { useState } from "react";
import { ListOrdered, Pencil, Plus } from "lucide-react";

import { ConfirmDialog } from "@/components/shared/confirm-dialog";
import { EmptyState } from "@/components/shared/empty-state";
import { ErrorState } from "@/components/shared/error-state";
import { PageHeader } from "@/components/shared/page-header";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { Switch } from "@/components/ui/switch";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { PriorityDialog } from "@/features/priorities/components/priority-dialog";
import { usePriorities, useSetPriorityStatus } from "@/features/priorities/hooks";
import type { PriorityResponse } from "@/features/priorities/api";

/** P10 — priority management: create / edit / activate / deactivate + level. */
export function AdminPrioritiesPage() {
  const query = usePriorities(false);
  const setStatus = useSetPriorityStatus();
  const [createOpen, setCreateOpen] = useState(false);
  const [editing, setEditing] = useState<PriorityResponse | null>(null);
  const [deactivating, setDeactivating] = useState<PriorityResponse | null>(null);

  const priorities = query.data ?? [];

  return (
    <div>
      <PageHeader
        title="Priorities"
        description="Ticket priorities from the AI analysis (lower level = more urgent)."
        actions={
          <Button onClick={() => setCreateOpen(true)}>
            <Plus aria-hidden="true" />
            New priority
          </Button>
        }
      />

      {query.isError ? (
        <ErrorState error={query.error} onRetry={() => void query.refetch()} />
      ) : (
        <div className="rounded-xl border bg-card">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Priority</TableHead>
                <TableHead>Level</TableHead>
                <TableHead>Active</TableHead>
                <TableHead className="text-right">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {query.isPending ? (
                Array.from({ length: 3 }).map((_, rowIndex) => (
                  <TableRow key={rowIndex}>
                    {Array.from({ length: 4 }).map((__, cellIndex) => (
                      <TableCell key={cellIndex}>
                        <Skeleton className="h-4 w-full max-w-32" />
                      </TableCell>
                    ))}
                  </TableRow>
                ))
              ) : priorities.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={4} className="p-0">
                    <EmptyState
                      icon={<ListOrdered className="size-10" aria-hidden="true" />}
                      title="No priorities yet"
                      description="Create priorities so the AI can rank incoming tickets."
                    />
                  </TableCell>
                </TableRow>
              ) : (
                priorities.map((priority) => (
                  <TableRow key={priority.id}>
                    <TableCell className="font-medium">{priority.name}</TableCell>
                    <TableCell className="tabular-nums">{priority.level}</TableCell>
                    <TableCell>
                      <Switch
                        checked={priority.isActive}
                        disabled={setStatus.isPending}
                        onCheckedChange={(next) => {
                          if (next) {
                            setStatus.mutate({ id: priority.id, isActive: true });
                          } else {
                            setDeactivating(priority);
                          }
                        }}
                        aria-label={`${
                          priority.isActive ? "Deactivate" : "Activate"
                        } priority ${priority.name}`}
                      />
                    </TableCell>
                    <TableCell className="text-right">
                      <Button variant="ghost" size="sm" onClick={() => setEditing(priority)}>
                        <Pencil aria-hidden="true" />
                        Edit
                      </Button>
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </div>
      )}

      <PriorityDialog open={createOpen} onOpenChange={setCreateOpen} />

      <PriorityDialog
        open={editing !== null}
        priority={editing ?? undefined}
        onOpenChange={(next) => {
          if (!next) {
            setEditing(null);
          }
        }}
      />

      <ConfirmDialog
        open={deactivating !== null}
        onOpenChange={(next) => {
          if (!next && !setStatus.isPending) {
            setDeactivating(null);
          }
        }}
        title="Deactivate this priority?"
        description={`"${deactivating?.name ?? ""}" will no longer be suggested by the AI for new tickets. Existing tickets keep it.`}
        confirmLabel="Deactivate"
        destructive
        loading={setStatus.isPending}
        onConfirm={() => {
          if (deactivating) {
            setStatus.mutate(
              { id: deactivating.id, isActive: false },
              { onSettled: () => setDeactivating(null) },
            );
          }
        }}
      />
    </div>
  );
}