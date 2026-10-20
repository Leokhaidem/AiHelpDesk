import { useMutation, useQuery, useQueryClient, type QueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import { queryKeys } from "@/lib/query-client";
import { toastApiError } from "@/lib/toast";

import { agentProfileApi } from "./api";

/** The own work profile (P13/P16). */
export function useAgentProfile() {
  return useQuery({
    queryKey: queryKeys.profile.me,
    queryFn: agentProfileApi.me,
    staleTime: 30_000,
  });
}

function invalidateProfileCache(queryClient: QueryClient) {
  void queryClient.invalidateQueries({ queryKey: ["agent"] });
  void queryClient.invalidateQueries({ queryKey: ["dashboard"] });
}

/**
 * Availability / capacity changes. `kind` only picks the success toast —
 * the request body is the same either way.
 */
export function useSetAgentAvailability() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (vars: { isAvailable: boolean; maxOpenTickets?: number; kind?: "availability" | "capacity" }) =>
      agentProfileApi.setAvailability({
        isAvailable: vars.isAvailable,
        maxOpenTickets: vars.maxOpenTickets,
      }),
    onSuccess: (_profile, vars) => {
      invalidateProfileCache(queryClient);
      if (vars.kind === "capacity") {
        toast.success("Capacity updated.");
        return;
      }
      toast.success(
        vars.isAvailable
          ? "You're now available — the assignment sweep will pick up eligible tickets."
          : "You're now unavailable — new tickets won't be assigned to you.",
      );
    },
    onError: (error) => toastApiError(error, "Failed to update your availability."),
  });
}

export function useSetAgentCategories() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (categoryIds: number[]) => agentProfileApi.setCategories(categoryIds),
    onSuccess: () => {
      invalidateProfileCache(queryClient);
      toast.success("Work categories updated.");
    },
    onError: (error) => toastApiError(error, "Failed to update the work categories."),
  });
}