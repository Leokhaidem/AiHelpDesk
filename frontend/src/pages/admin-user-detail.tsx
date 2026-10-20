import { useEffect, useState, type ReactNode } from "react";
import { Link, useParams } from "react-router";
import { ArrowLeft, UserRoundSearch } from "lucide-react";

import { CategoryMultiSelect } from "@/components/shared/category-multi-select";
import { ConfirmDialog } from "@/components/shared/confirm-dialog";
import { EmptyState } from "@/components/shared/empty-state";
import { ErrorState } from "@/components/shared/error-state";
import { PageHeader } from "@/components/shared/page-header";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Skeleton } from "@/components/ui/skeleton";
import { useCategories } from "@/features/categories/hooks";
import { RoleChangeDialog } from "@/features/users/components/role-change-dialog";
import { UserEditDialog } from "@/features/users/components/user-edit-dialog";
import {
  useSetUserCategories,
  useSetUserStatus,
  useUser,
  useUserCategories,
} from "@/features/users/hooks";
import { isApiError } from "@/lib/error-mapping";
import { useAuthStore } from "@/stores/auth-store";

function MetaRow({ label, value }: { label: string; value: ReactNode }) {
  return (
    <div className="flex items-baseline justify-between gap-4 py-1.5">
      <dt className="shrink-0 text-muted-foreground">{label}</dt>
      <dd className="text-right font-medium break-words">{value}</dd>
    </div>
  );
}

/** P8 — one user's profile, status, role and (for agents) work categories. */
export function AdminUserDetailPage() {
  const { id } = useParams();
  const userId = id ?? "";
  const userQuery = useUser(userId, userId.length > 0);
  const user = userQuery.data;
  const isAgent = user?.roles.includes("Agent") ?? false;
  const isAdminUser = user?.roles.includes("Admin") ?? false;
  const currentUserId = useAuthStore((state) => state.user?.id ?? "");
  const isSelf = user !== undefined && user.id === currentUserId;

  const categoriesQuery = useUserCategories(userId, isAgent);
  const allCategories = useCategories(true);
  const setUserStatus = useSetUserStatus();
  const setUserCategories = useSetUserCategories();

  const [editOpen, setEditOpen] = useState(false);
  const [roleOpen, setRoleOpen] = useState(false);
  const [deactivateOpen, setDeactivateOpen] = useState(false);
  const [selectedCategories, setSelectedCategories] = useState<number[]>([]);

  useEffect(() => {
    if (isAgent) {
      setSelectedCategories((categoriesQuery.data ?? []).map((c) => c.id));
    }
  }, [isAgent, categoriesQuery.data]);

  if (!userId) {
    return (
      <div>
        <PageHeader title="User" />
        <EmptyState
          title="User not found"
          description="The user ID in the address bar is not valid."
        />
      </div>
    );
  }

  if (userQuery.isPending) {
    return (
      <div>
        <PageHeader title="User" />
        <div className="space-y-4" aria-busy="true" aria-label="Loading user">
          <Skeleton className="h-40 w-full" />
          <Skeleton className="h-40 w-full" />
        </div>
      </div>
    );
  }

  if (userQuery.isError || !user) {
    if (isApiError(userQuery.error) && userQuery.error.status === 404) {
      return (
        <div>
          <PageHeader title="User" />
          <EmptyState
            icon={<UserRoundSearch className="size-10" aria-hidden="true" />}
            title="User not found"
            description="This user does not exist or may have been removed."
            action={
              <Button asChild variant="outline">
                <Link to="/admin/users">Back to users</Link>
              </Button>
            }
          />
        </div>
      );
    }
    return (
      <div>
        <PageHeader title="User" />
        <ErrorState error={userQuery.error} onRetry={() => void userQuery.refetch()} />
      </div>
    );
  }

  const currentIds = (categoriesQuery.data ?? []).map((c) => c.id);
  const categoriesChanged =
    isAgent &&
    selectedCategories.length > 0 &&
    [...selectedCategories].sort((a, b) => a - b).join(",") !==
      [...currentIds].sort((a, b) => a - b).join(",");

  return (
    <div>
      <div className="mb-3">
        <Link
          to="/admin/users"
          className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
        >
          <ArrowLeft className="size-4" aria-hidden="true" />
          Back to users
        </Link>
      </div>
      <PageHeader
        title={user.fullName}
        description={user.email}
        actions={
          user.isActive ? (
            <Button
              variant="destructive"
              disabled={isSelf}
              title={isSelf ? "You cannot deactivate your own account" : undefined}
              onClick={() => setDeactivateOpen(true)}
            >
              Deactivate
            </Button>
          ) : (
            <Button
              disabled={setUserStatus.isPending}
              onClick={() => setUserStatus.mutate({ id: userId, isActive: true })}
            >
              Activate
            </Button>
          )
        }
      />

      <div className="grid items-start gap-4 lg:grid-cols-2">
        <section className="rounded-lg border bg-card p-4">
          <div className="mb-2 flex items-center justify-between">
            <h2 className="text-sm font-medium text-muted-foreground">Profile</h2>
            <Button variant="outline" size="sm" onClick={() => setEditOpen(true)}>
              Edit profile
            </Button>
          </div>
          <dl className="divide-y">
            <MetaRow label="Full name" value={user.fullName} />
            <MetaRow label="Email" value={user.email} />
            <MetaRow
              label="Roles"
              value={
                <span className="flex flex-wrap justify-end gap-1">
                  {user.roles.map((r) => (
                    <Badge key={r} variant="outline" className="text-xs font-normal">
                      {r}
                    </Badge>
                  ))}
                </span>
              }
            />
            <MetaRow
              label="Status"
              value={
                user.isActive ? (
                  <Badge
                    variant="secondary"
                    className="border-emerald-300/60 bg-emerald-50 text-emerald-800 dark:border-emerald-500/30 dark:bg-emerald-500/10 dark:text-emerald-300"
                  >
                    Active
                  </Badge>
                ) : (
                  <Badge
                    variant="secondary"
                    className="border-border bg-muted text-muted-foreground"
                  >
                    Inactive
                  </Badge>
                )
              }
            />
          </dl>
          {isSelf && user.isActive ? (
            <p className="mt-3 text-xs text-muted-foreground">
              You cannot deactivate your own account.
            </p>
          ) : null}
          <div className="mt-4">
            {isAdminUser ? (
              <p className="text-xs text-muted-foreground">
                Admin accounts cannot be managed through this page.
              </p>
            ) : (
              <Button variant="outline" size="sm" onClick={() => setRoleOpen(true)}>
                Change role…
              </Button>
            )}
          </div>
        </section>

        {isAgent ? (
          <section className="rounded-lg border bg-card p-4">
            <h2 className="mb-2 text-sm font-medium text-muted-foreground">Agent profile</h2>
            <p className="text-sm">
              Open tickets:{" "}
              <span className="font-medium tabular-nums">{user.openTicketCount ?? 0}</span>
            </p>
            <div className="mt-4">
              <Label id="user-detail-categories-label">Work categories</Label>
              <CategoryMultiSelect
                id="user-detail-categories"
                aria-labelledby="user-detail-categories-label"
                className="mt-1.5"
                value={selectedCategories}
                onValueChange={setSelectedCategories}
                options={(allCategories.data ?? []).map((c) => ({ id: c.id, name: c.name }))}
                placeholder="Select the categories this agent works"
                disabled={setUserCategories.isPending || categoriesQuery.isPending}
              />
            </div>
            <div className="mt-3">
              <Button
                size="sm"
                disabled={!categoriesChanged || setUserCategories.isPending}
                onClick={() =>
                  setUserCategories.mutate({ id: userId, categoryIds: selectedCategories })
                }
              >
                Save categories
              </Button>
            </div>
          </section>
        ) : null}
      </div>

      <UserEditDialog user={user} open={editOpen} onOpenChange={setEditOpen} />

      {!isAdminUser ? (
        <RoleChangeDialog
          user={user}
          open={roleOpen}
          onOpenChange={setRoleOpen}
          currentCategories={currentIds}
        />
      ) : null}

      <ConfirmDialog
        open={deactivateOpen}
        onOpenChange={setDeactivateOpen}
        title={`Deactivate ${user.fullName}?`}
        description={
          isAgent
            ? "They are an active Agent. Deactivating requeues their open Assigned tickets to the assignment queue and flags any InProgress tickets for manual review."
            : "They will no longer be able to sign in until they are reactivated."
        }
        summary={
          isAgent && (user.openTicketCount ?? 0) > 0
            ? [{ label: "Open tickets", value: user.openTicketCount ?? 0 }]
            : undefined
        }
        confirmLabel="Deactivate"
        destructive
        loading={setUserStatus.isPending}
        onConfirm={() => {
          setUserStatus.mutate(
            { id: userId, isActive: false },
            { onSettled: () => setDeactivateOpen(false) },
          );
        }}
      />
    </div>
  );
}