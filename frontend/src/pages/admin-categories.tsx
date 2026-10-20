import { useState } from "react";
import { FolderOpen, Pencil, Plus } from "lucide-react";

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
import { CategoryDialog } from "@/features/categories/components/category-dialog";
import { useCategories, useSetCategoryStatus } from "@/features/categories/hooks";
import type { CategoryResponse } from "@/features/categories/api";

/** P9 — category management: create / edit / activate / deactivate. */
export function AdminCategoriesPage() {
  const query = useCategories(false);
  const setStatus = useSetCategoryStatus();
  const [createOpen, setCreateOpen] = useState(false);
  const [editing, setEditing] = useState<CategoryResponse | null>(null);
  const [deactivating, setDeactivating] = useState<CategoryResponse | null>(null);

  const categories = query.data ?? [];

  return (
    <div>
      <PageHeader
        title="Categories"
        description="Ticket categories drive AI analysis and agent assignment."
        actions={
          <Button onClick={() => setCreateOpen(true)}>
            <Plus aria-hidden="true" />
            New category
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
                <TableHead>Name</TableHead>
                <TableHead>Description</TableHead>
                <TableHead>Agents</TableHead>
                <TableHead>Active</TableHead>
                <TableHead className="text-right">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {query.isPending ? (
                Array.from({ length: 4 }).map((_, rowIndex) => (
                  <TableRow key={rowIndex}>
                    {Array.from({ length: 5 }).map((__, cellIndex) => (
                      <TableCell key={cellIndex}>
                        <Skeleton className="h-4 w-full max-w-32" />
                      </TableCell>
                    ))}
                  </TableRow>
                ))
              ) : categories.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={5} className="p-0">
                    <EmptyState
                      icon={<FolderOpen className="size-10" aria-hidden="true" />}
                      title="No categories yet"
                      description="Create the first category so the AI has something to analyze against."
                    />
                  </TableCell>
                </TableRow>
              ) : (
                categories.map((category) => (
                  <TableRow key={category.id}>
                    <TableCell className="font-medium">{category.name}</TableCell>
                    <TableCell className="max-w-xs truncate">{category.description ?? "—"}</TableCell>
                    <TableCell className="tabular-nums">{category.agents.length}</TableCell>
                    <TableCell>
                      <Switch
                        checked={category.isActive}
                        disabled={setStatus.isPending}
                        onCheckedChange={(next) => {
                          if (next) {
                            setStatus.mutate({ id: category.id, isActive: true });
                          } else {
                            setDeactivating(category);
                          }
                        }}
                        aria-label={`${
                          category.isActive ? "Deactivate" : "Activate"
                        } category ${category.name}`}
                      />
                    </TableCell>
                    <TableCell className="text-right">
                      <Button variant="ghost" size="sm" onClick={() => setEditing(category)}>
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

      <CategoryDialog open={createOpen} onOpenChange={setCreateOpen} />

      <CategoryDialog
        open={editing !== null}
        category={editing ?? undefined}
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
        title="Deactivate this category?"
        description={`"${deactivating?.name ?? ""}" will no longer be offered for new tickets or agent profiles. Existing tickets keep it.`}
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