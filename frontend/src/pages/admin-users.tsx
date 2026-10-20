import { useState } from "react";
import { useNavigate } from "react-router";
import { Plus, UsersRound } from "lucide-react";

import { EmptyState } from "@/components/shared/empty-state";
import { ErrorState } from "@/components/shared/error-state";
import { FilterBar } from "@/components/shared/filter-bar";
import { PageHeader } from "@/components/shared/page-header";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { CreateUserDialog } from "@/features/users/components/create-user-dialog";
import { useUsers } from "@/features/users/hooks";
import type { AdminRole } from "@/features/users/api";
import { useClearParams, useUrlParam } from "@/lib/url-filters";

/** P7 — all users with role filter + client-side text search (API is not paginated). */
export function AdminUsersPage() {
  const navigate = useNavigate();
  const [roleParam, setRoleParam] = useUrlParam("role");
  const [searchParam, setSearchParam] = useUrlParam("search");
  const [createOpen, setCreateOpen] = useState(false);
  const clearFilters = useClearParams();

  const role: AdminRole | undefined =
    roleParam === "Employee" || roleParam === "Agent" ? roleParam : undefined;
  const query = useUsers(role);

  const search = searchParam?.trim().toLowerCase();
  const users = (query.data ?? []).filter(
    (user) =>
      !search ||
      user.fullName.toLowerCase().includes(search) ||
      user.email.toLowerCase().includes(search),
  );

  return (
    <div>
      <PageHeader
        title="Users"
        description="Manage Employee and Agent accounts."
        actions={
          <Button onClick={() => setCreateOpen(true)}>
            <Plus aria-hidden="true" />
            Create user
          </Button>
        }
      />

      <FilterBar
        hasActiveFilters={Boolean(roleParam || searchParam)}
        onClearFilters={() => clearFilters(["role", "search"])}
      >
        <div className="flex flex-col gap-1">
          <Label htmlFor="users-role">Role</Label>
          <Select
            value={role ?? "all"}
            onValueChange={(value) => setRoleParam(value === "all" ? undefined : value)}
          >
            <SelectTrigger id="users-role" className="w-40">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All roles</SelectItem>
              <SelectItem value="Employee">Employee</SelectItem>
              <SelectItem value="Agent">Agent</SelectItem>
            </SelectContent>
          </Select>
        </div>
        <div className="flex flex-col gap-1">
          <Label htmlFor="users-search">Search</Label>
          <Input
            id="users-search"
            type="search"
            className="w-64"
            placeholder="Name or email"
            value={searchParam ?? ""}
            onChange={(event) => setSearchParam(event.target.value || undefined)}
          />
        </div>
      </FilterBar>

      {query.isError ? (
        <ErrorState error={query.error} onRetry={() => void query.refetch()} />
      ) : (
        <div className="rounded-xl border bg-card">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Name</TableHead>
                <TableHead>Email</TableHead>
                <TableHead>Roles</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Agent load</TableHead>
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
              ) : users.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={5} className="p-0">
                    <EmptyState
                      icon={<UsersRound className="size-10" aria-hidden="true" />}
                      title={search ? "No users match your filters" : "No users found"}
                      description={
                        search
                          ? "Try adjusting the role filter or search text."
                          : "Users you create will appear here."
                      }
                    />
                  </TableCell>
                </TableRow>
              ) : (
                users.map((user) => (
                  <TableRow
                    key={user.id}
                    className="cursor-pointer"
                    onClick={() => navigate(`/admin/users/${user.id}`)}
                  >
                    <TableCell className="font-medium">{user.fullName}</TableCell>
                    <TableCell>{user.email}</TableCell>
                    <TableCell>
                      <div className="flex flex-wrap gap-1">
                        {user.roles.map((r) => (
                          <Badge key={r} variant="outline" className="text-xs font-normal">
                            {r}
                          </Badge>
                        ))}
                      </div>
                    </TableCell>
                    <TableCell>
                      {user.isActive ? (
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
                      )}
                    </TableCell>
                    <TableCell>
                      {user.roles.includes("Agent") ? (
                        <span className="tabular-nums">
                          {user.openTicketCount ?? 0} open · {user.categories?.length ?? 0}{" "}
                          categories
                        </span>
                      ) : (
                        "—"
                      )}
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </div>
      )}

      <CreateUserDialog open={createOpen} onOpenChange={setCreateOpen} />
    </div>
  );
}