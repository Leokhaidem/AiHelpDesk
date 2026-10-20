import axios from "axios";

import { toApiError } from "@/lib/error-mapping";
import { useAuthStore } from "@/stores/auth-store";

export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5045";

/**
 * Single axios instance for the whole app.
 *  - attaches the Bearer token from the auth store
 *  - treats 401 as "re-authentication required" (expiry OR live deactivation,
 *    integration doc section 4.4) unless the failing request is an auth call
 *    or a deliberate logout is in progress
 *  - normalizes every rejection to ApiError
 */
export const apiClient = axios.create({
  baseURL: API_BASE_URL,
  timeout: 15_000,
});

apiClient.interceptors.request.use((config) => {
  const token = useAuthStore.getState().token;
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

apiClient.interceptors.response.use(
  (response) => response,
  (error: unknown) => {
    const apiError = toApiError(error);

    const url =
      error !== null && typeof error === "object" && "config" in error
        ? (error as { config?: { url?: string } }).config?.url
        : undefined;
    const isAuthCall = url?.includes("/auth/") ?? false;
    const isLoggingOut = useAuthStore.getState().isLoggingOut;

    if (apiError.status === 401 && !isAuthCall && !isLoggingOut) {
      useAuthStore.getState().logout();
      const next = window.location.pathname + window.location.search;
      window.location.assign(`/login?next=${encodeURIComponent(next)}`);
    }

    return Promise.reject(apiError);
  },
);
