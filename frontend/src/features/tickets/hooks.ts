import { useMutation, useQuery, useQueryClient, type QueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import { queryKeys } from "@/lib/query-client";
import { toastApiError } from "@/lib/toast";
import type { TicketStatus } from "@/types/api";

import { ticketsApi, type TicketListParams } from "./api";

export type TicketScope = "employee" | "agent" | "admin";

const LIST_REFETCH_INTERVAL = 30_000;
const DETAIL_POLL_INTERVAL = 5_000;

/**
 * Scoped ticket list with previous-page data kept visible while the next
 * page loads (smooth pagination) and a 30 s background refetch while the
 * page is open (no push channel — plan §11).
 */
export function useTicketList(scope: TicketScope, params: TicketListParams) {
  return useQuery({
    queryKey: queryKeys.tickets.list(scope, params),
    queryFn: () => ticketsApi[scope].list(params),
    placeholderData: (previousData) => previousData,
    refetchInterval: LIST_REFETCH_INTERVAL,
  });
}

/**
 * Ticket detail with conditional polling (plan §11): every 5 s while the
 * AI is still working (Created / AnalysisInProgress) — and, for the admin
 * scope, while AwaitingAssignment, since the assignment sweep may pick an
 * agent at any time. Off for all other statuses.
 */
export function useTicketDetail(ticketId: number, scope: TicketScope, enabled = true) {
  return useQuery({
    queryKey: queryKeys.tickets.detail(scope, String(ticketId)),
    queryFn: () => ticketsApi[scope].get(ticketId),
    enabled: enabled && ticketId > 0,
    refetchInterval: (query) => {
      const status: TicketStatus | undefined = query.state.data?.status;
      const active =
        status === "Created" ||
        status === "AnalysisInProgress" ||
        (scope === "admin" && status === "AwaitingAssignment");
      return active ? DETAIL_POLL_INTERVAL : false;
    },
  });
}

export function useTicketComments(ticketId: number, enabled = true) {
  return useQuery({
    queryKey: queryKeys.tickets.comments(String(ticketId)),
    queryFn: () => ticketsApi.comments.list(ticketId),
    enabled: enabled && ticketId > 0,
  });
}

/** Admin audit timeline (P6). */
export function useTicketHistory(ticketId: number, enabled = true) {
  return useQuery({
    queryKey: queryKeys.tickets.history(String(ticketId)),
    queryFn: () => ticketsApi.history.list(ticketId),
    enabled: enabled && ticketId > 0,
  });
}

/**
 * Any ticket mutation refreshes the whole ticket cache — lists, details,
 * comments and history — plus the dashboard and reports, which aggregate
 * ticket data (plan section 4.3 invalidation matrix).
 */
function invalidateTicketCache(queryClient: QueryClient) {
  void queryClient.invalidateQueries({ queryKey: ["tickets"] });
  void queryClient.invalidateQueries({ queryKey: ["dashboard"] });
  void queryClient.invalidateQueries({ queryKey: ["reports"] });
}

export function useCreateTicket() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (vars: { title: string; description: string }) => ticketsApi.employee.create(vars),
    onSuccess: () => {
      invalidateTicketCache(queryClient);
    },
    onError: (error) => toastApiError(error, "Failed to create the ticket."),
  });
}

/** Assigned → InProgress (agent). */
export function useStartTicket() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (ticketId: number) => ticketsApi.agent.start(ticketId),
    onSuccess: () => {
      invalidateTicketCache(queryClient);
      toast.success("Ticket is now in progress.");
    },
    onError: (error) => toastApiError(error, "Failed to start the ticket."),
  });
}

/** InProgress → Resolved (agent). */
export function useResolveTicket() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (vars: { ticketId: number; resolution: string }) =>
      ticketsApi.agent.resolve(vars.ticketId, vars.resolution),
    onSuccess: () => {
      invalidateTicketCache(queryClient);
      toast.success("Ticket resolved.");
    },
    onError: (error) => toastApiError(error, "Failed to resolve the ticket."),
  });
}

/** Resolved → Closed (agent). */
export function useCloseTicket() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (ticketId: number) => ticketsApi.agent.close(ticketId),
    onSuccess: () => {
      invalidateTicketCache(queryClient);
      toast.success("Ticket closed.");
    },
    onError: (error) => toastApiError(error, "Failed to close the ticket."),
  });
}

/** Assigned → AwaitingAssignment (agent). */
export function useReleaseTicket() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (ticketId: number) => ticketsApi.agent.release(ticketId),
    onSuccess: () => {
      invalidateTicketCache(queryClient);
      toast.success("Ticket released back to the queue.");
    },
    onError: (error) => toastApiError(error, "Failed to release the ticket."),
  });
}

/** Admin: assign an agent to a ticket. */
export function useAssignAgent() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (vars: { ticketId: number; agentId: string }) =>
      ticketsApi.admin.assignAgent(vars.ticketId, vars.agentId),
    onSuccess: () => {
      invalidateTicketCache(queryClient);
      toast.success("Agent assigned.");
    },
    onError: (error) => toastApiError(error, "Failed to assign the agent."),
  });
}

/** Admin: manually set category + priority on a failed analysis. */
export function useOverrideAnalysis() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (vars: { ticketId: number; categoryId: number; priorityId: number; agentId?: string }) =>
      ticketsApi.admin.overrideAnalysis(vars.ticketId, {
        categoryId: vars.categoryId,
        priorityId: vars.priorityId,
        agentId: vars.agentId,
      }),
    onSuccess: () => {
      invalidateTicketCache(queryClient);
      toast.success("Analysis overridden.");
    },
    onError: (error) => toastApiError(error, "Failed to override the analysis."),
  });
}

/** Admin: re-run the AI analysis on a failed ticket. */
export function useRetryAnalysis() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (ticketId: number) => ticketsApi.admin.retryAnalysis(ticketId),
    onSuccess: () => {
      invalidateTicketCache(queryClient);
      toast.success("AI analysis queued for retry.");
    },
    onError: (error) => toastApiError(error, "Failed to retry the analysis."),
  });
}

/** Admin: close any resolved ticket. */
export function useAdminCloseTicket() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (ticketId: number) => ticketsApi.admin.close(ticketId),
    onSuccess: () => {
      invalidateTicketCache(queryClient);
      toast.success("Ticket closed.");
    },
    onError: (error) => toastApiError(error, "Failed to close the ticket."),
  });
}

export function useAddComment() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (vars: { ticketId: number; content: string }) =>
      ticketsApi.comments.create(vars.ticketId, vars.content),
    onSuccess: () => {
      invalidateTicketCache(queryClient);
      toast.success("Comment added.");
    },
    onError: (error) => toastApiError(error, "Failed to add the comment."),
  });
}
