import { useMutation, useQuery, useQueryClient, type QueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import { queryKeys } from "@/lib/query-client";
import { toastApiError } from "@/lib/toast";

import { prioritiesApi, type CreatePriorityRequest } from "./api";

const LOOKUP_STALE_TIME = 5 * 60 * 1000;

function invalidateLookups(queryClient: QueryClient) {
  void queryClient.invalidateQueries({ queryKey: ["categories"] });
  void queryClient.invalidateQueries({ queryKey: ["priorities"] });
  void queryClient.invalidateQueries({ queryKey: ["agent"] });
  void queryClient.invalidateQueries({ queryKey: ["dashboard"] });
}

/** Active priorities for any authenticated user (ticket filters, forms). */
export function usePublicPriorities() {
  return useQuery({
    queryKey: queryKeys.priorities.public,
    queryFn: prioritiesApi.publicList,
    staleTime: LOOKUP_STALE_TIME,
  });
}

/** Admin list including inactive priorities (P10). */
export function usePriorities(activeOnly = false) {
  return useQuery({
    queryKey: queryKeys.priorities.all(activeOnly),
    queryFn: () => prioritiesApi.list(activeOnly),
    staleTime: LOOKUP_STALE_TIME,
  });
}

export function useCreatePriority() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (body: CreatePriorityRequest) => prioritiesApi.create(body),
    onSuccess: () => {
      invalidateLookups(queryClient);
      toast.success("Priority created.");
    },
    onError: (error) => toastApiError(error, "Failed to create the priority."),
  });
}

export function useUpdatePriority() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (vars: { id: number; name: string; level: number }) =>
      prioritiesApi.update(vars.id, { name: vars.name, level: vars.level }),
    onSuccess: () => {
      invalidateLookups(queryClient);
      toast.success("Priority updated.");
    },
    onError: (error) => toastApiError(error, "Failed to update the priority."),
  });
}

export function useSetPriorityStatus() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (vars: { id: number; isActive: boolean }) =>
      prioritiesApi.setStatus(vars.id, vars.isActive),
    onSuccess: (_data, vars) => {
      invalidateLookups(queryClient);
      toast.success(vars.isActive ? "Priority activated." : "Priority deactivated.");
    },
    onError: (error) => toastApiError(error, "Failed to update the priority status."),
  });
}
