import { useMutation, useQuery, useQueryClient, type QueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import { queryKeys } from "@/lib/query-client";
import { toastApiError } from "@/lib/toast";

import {
  usersApi,
  type AdminRole,
  type AgentDeactivationResult,
  type CreateAdminUserRequest,
} from "./api";

const LOOKUP_STALE_TIME = 5 * 60 * 1000;

/**
 * User list for the admin area (P7). The endpoint is not paginated, so
 * search / role filtering combine server role filtering with client-side
 * text search (plan §16).
 */
export function useUsers(role?: AdminRole) {
  return useQuery({
    queryKey: queryKeys.users.all(role),
    queryFn: () => usersApi.list(role),
    staleTime: LOOKUP_STALE_TIME,
  });
}

export function useUser(id: string, enabled = true) {
  return useQuery({
    queryKey: queryKeys.users.byId(id),
    queryFn: () => usersApi.getById(id),
    enabled: enabled && id.length > 0,
    staleTime: LOOKUP_STALE_TIME,
  });
}

/** Work categories of an Agent user (P8 editor). */
export function useUserCategories(id: string, enabled = true) {
  return useQuery({
    queryKey: queryKeys.users.categories(id),
    queryFn: () => usersApi.getCategories(id),
    enabled: enabled && id.length > 0,
    staleTime: LOOKUP_STALE_TIME,
  });
}

/**
 * Every user in the Agent role (active and deactivated) — the data for the
 * admin ticket list agent filter. The old `/agent-applications/active`
 * endpoint does not exist; `/admin/users?role=Agent` is the source.
 */
export function useAgents(enabled = true) {
  return useQuery({
    queryKey: queryKeys.users.agents,
    queryFn: () => usersApi.list("Agent"),
    staleTime: LOOKUP_STALE_TIME,
    enabled,
  });
}

/**
 * Active agents only — the admin assign flow (a deactivated agent is
 * rejected with a 400 by the backend).
 */
export function useActiveAgents(enabled = true) {
  return useQuery({
    queryKey: queryKeys.users.agents,
    queryFn: () => usersApi.list("Agent"),
    staleTime: LOOKUP_STALE_TIME,
    enabled,
    select: (users) =>
      users
        .filter((user) => user.isActive)
        .map((user) => ({ id: user.id, fullName: user.fullName })),
  });
}

function invalidateUserCache(queryClient: QueryClient) {
  void queryClient.invalidateQueries({ queryKey: ["users"] });
  void queryClient.invalidateQueries({ queryKey: ["dashboard"] });
}

/** Describes the Flow I outcome in a toast (deactivation / demotion). */
function describeDeactivation(result: AgentDeactivationResult): string {
  return `${result.requeuedTicketCount} open tickets requeued, ${result.flaggedTicketCount} in-progress tickets flagged for review, ${result.autoAssignedTicketCount} auto-assigned to other agents.`;
}

export function useCreateUser() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (body: CreateAdminUserRequest) => usersApi.create(body),
    onSuccess: () => {
      invalidateUserCache(queryClient);
      toast.success("User created.");
    },
    onError: (error) => toastApiError(error, "Failed to create the user."),
  });
}

export function useUpdateUserProfile() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (vars: { id: string; fullName: string; email: string }) =>
      usersApi.update(vars.id, { fullName: vars.fullName, email: vars.email }),
    onSuccess: () => {
      invalidateUserCache(queryClient);
      toast.success("Profile updated.");
    },
    onError: (error) => toastApiError(error, "Failed to update the profile."),
  });
}

export function useSetUserStatus() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (vars: { id: string; isActive: boolean }) =>
      usersApi.setStatus(vars.id, vars.isActive),
    onSuccess: (result, vars) => {
      invalidateUserCache(queryClient);
      if (!vars.isActive && result.agentDeactivation) {
        toast.success(`User deactivated — ${describeDeactivation(result.agentDeactivation)}.`);
      } else {
        toast.success(vars.isActive ? "User activated." : "User deactivated.");
      }
    },
    onError: (error) => toastApiError(error, "Failed to update the user status."),
  });
}

export function useSetUserRole() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (vars: { id: string; role: AdminRole; categoryIds?: number[] }) =>
      usersApi.setRole(vars.id, vars.role, vars.categoryIds),
    onSuccess: (result) => {
      invalidateUserCache(queryClient);
      if (result.agentDeactivation) {
        toast.success(`Role change requeued work — ${describeDeactivation(result.agentDeactivation)}.`);
      }
      toast.success("Role updated — the user must sign in again to pick up the new role.");
    },
    onError: (error) => toastApiError(error, "Failed to update the user role."),
  });
}

export function useSetUserCategories() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (vars: { id: string; categoryIds: number[] }) =>
      usersApi.setCategories(vars.id, vars.categoryIds),
    onSuccess: () => {
      invalidateUserCache(queryClient);
      // The agent profile exposes the same category set.
      void queryClient.invalidateQueries({ queryKey: ["agent"] });
      toast.success("Work categories updated.");
    },
    onError: (error) => toastApiError(error, "Failed to update the work categories."),
  });
}