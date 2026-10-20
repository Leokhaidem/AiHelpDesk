import { useMutation, useQuery, useQueryClient, type QueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import { isApiError } from "@/lib/error-mapping";
import { queryKeys } from "@/lib/query-client";
import { toastApiError } from "@/lib/toast";

import {
  adminApplicationsApi,
  agentApplicationsApi,
  type AgentApplicationResponse,
  type ApplicationStatus,
  type CreateApplicationRequest,
} from "./api";

const LOOKUP_STALE_TIME = 5 * 60 * 1000;

/**
 * The own most recent agent application (P17/P21). A 404 means "never
 * applied" and is surfaced as `data: null` instead of an error.
 */
export function useMyApplication(enabled = true) {
  return useQuery({
    queryKey: queryKeys.applications.mine,
    queryFn: async (): Promise<AgentApplicationResponse | null> => {
      try {
        return await agentApplicationsApi.mine();
      } catch (error) {
        if (isApiError(error) && error.status === 404) {
          return null;
        }
        throw error;
      }
    },
    enabled,
    staleTime: LOOKUP_STALE_TIME,
  });
}

export function useCreateApplication() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (body: CreateApplicationRequest) => agentApplicationsApi.create(body),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["applications"] });
      void queryClient.invalidateQueries({ queryKey: ["dashboard"] });
      toast.success("Your agent application has been submitted for review.");
    },
    onError: (error) => toastApiError(error, "Failed to submit the application."),
  });
}

export function useWithdrawApplication() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => agentApplicationsApi.withdraw(),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["applications"] });
      toast.success("Your application has been withdrawn.");
    },
    onError: (error) => toastApiError(error, "Failed to withdraw the application."),
  });
}

/**
 * Admin review writes invalidate the review queues, the users list (a
 * pending application may have flipped the user's role) and the dashboard
 * (plan §4.3 invalidation matrix).
 */
function invalidateApplicationReviews(queryClient: QueryClient) {
  void queryClient.invalidateQueries({ queryKey: ["applications"] });
  void queryClient.invalidateQueries({ queryKey: ["users"] });
  void queryClient.invalidateQueries({ queryKey: ["dashboard"] });
}

/** P11 — admin review queue, paged by status (server orders newest first). */
export function useAdminApplications(status: ApplicationStatus, page: number, pageSize: number) {
  return useQuery({
    queryKey: queryKeys.applications.adminList(status, page, pageSize),
    queryFn: () => adminApplicationsApi.list({ status, page, pageSize }),
  });
}

/** P11 — approve; empty categoryIds falls back to the requested ones. */
export function useApproveApplication() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (vars: { id: number; categoryIds?: number[]; note?: string }) =>
      adminApplicationsApi.approve(vars.id, {
        categoryIds:
          vars.categoryIds && vars.categoryIds.length > 0 ? vars.categoryIds : undefined,
        note: vars.note?.trim() ? vars.note.trim() : undefined,
      }),
    onSuccess: () => {
      invalidateApplicationReviews(queryClient);
      toast.success(
        "Application approved — the applicant must sign in again to use the agent area.",
      );
    },
    onError: (error) => {
      // 409 = no longer pending: refresh the queue; the page shows an inline notice.
      if (isApiError(error) && error.status === 409) {
        void queryClient.invalidateQueries({ queryKey: ["applications"] });
      }
      toastApiError(error, "Failed to approve the application.");
    },
  });
}

/** P11 — reject with optional note. */
export function useRejectApplication() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (vars: { id: number; note?: string }) =>
      adminApplicationsApi.reject(vars.id, {
        note: vars.note?.trim() ? vars.note.trim() : undefined,
      }),
    onSuccess: () => {
      invalidateApplicationReviews(queryClient);
      toast.success("Application rejected.");
    },
    onError: (error) => {
      if (isApiError(error) && error.status === 409) {
        void queryClient.invalidateQueries({ queryKey: ["applications"] });
      }
      toastApiError(error, "Failed to reject the application.");
    },
  });
}
