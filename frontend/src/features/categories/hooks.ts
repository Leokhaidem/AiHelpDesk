import { useMutation, useQuery, useQueryClient, type QueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import { queryKeys } from "@/lib/query-client";
import { toastApiError } from "@/lib/toast";

import { categoriesApi, type CreateCategoryRequest, type UpdateCategoryRequest } from "./api";

const LOOKUP_STALE_TIME = 5 * 60 * 1000;

/**
 * Category writes invalidate every cache that serves categories: the
 * lookup lists, the agent profile (its own categories) and the dashboard
 * (plan §4.3 invalidation matrix).
 */
function invalidateLookups(queryClient: QueryClient) {
  void queryClient.invalidateQueries({ queryKey: ["categories"] });
  void queryClient.invalidateQueries({ queryKey: ["priorities"] });
  void queryClient.invalidateQueries({ queryKey: ["agent"] });
  void queryClient.invalidateQueries({ queryKey: ["dashboard"] });
}

export function usePublicCategories() {
  return useQuery({
    queryKey: queryKeys.categories.public,
    queryFn: categoriesApi.publicList,
    staleTime: LOOKUP_STALE_TIME,
  });
}

export function useCategories(activeOnly = false) {
  return useQuery({
    queryKey: queryKeys.categories.all(activeOnly),
    queryFn: () => categoriesApi.list(activeOnly),
    staleTime: LOOKUP_STALE_TIME,
  });
}

export function useCreateCategory() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (body: CreateCategoryRequest) => categoriesApi.create(body),
    onSuccess: () => {
      invalidateLookups(queryClient);
      toast.success("Category created.");
    },
    onError: (error) => toastApiError(error, "Failed to create the category."),
  });
}

export function useUpdateCategory() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (vars: { id: number; name: string; description?: string }) =>
      categoriesApi.update(vars.id, {
        name: vars.name,
        description: vars.description,
      } satisfies UpdateCategoryRequest),
    onSuccess: () => {
      invalidateLookups(queryClient);
      toast.success("Category updated.");
    },
    onError: (error) => toastApiError(error, "Failed to update the category."),
  });
}

export function useSetCategoryStatus() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (vars: { id: number; isActive: boolean }) =>
      categoriesApi.setStatus(vars.id, vars.isActive),
    onSuccess: (_data, vars) => {
      invalidateLookups(queryClient);
      toast.success(vars.isActive ? "Category activated." : "Category deactivated.");
    },
    onError: (error) => toastApiError(error, "Failed to update the category status."),
  });
}
