import type { ReactNode } from "react";
import { Navigate, useLocation } from "react-router";

import { useAuthStore } from "@/stores/auth-store";

/**
 * Route guards are UX only — the server is the enforcement point (plan
 * section 6). A missing session goes to /login?next=...; a present session
 * without the required roles goes to /unauthorized.
 */
export function ProtectedRoute({ children }: { children: ReactNode }) {
  const status = useAuthStore((state) => state.status);
  const location = useLocation();

  if (status !== "authenticated") {
    const next = encodeURIComponent(location.pathname + location.search);
    return <Navigate to={`/login?next=${next}`} replace />;
  }

  return <>{children}</>;
}

export function RequireRole({ roles, children }: { roles: string[]; children: ReactNode }) {
  const user = useAuthStore((state) => state.user);
  const hasRole = user?.roles.some((role) => roles.includes(role)) ?? false;

  if (!hasRole) {
    return <Navigate to="/unauthorized" replace />;
  }

  return <>{children}</>;
}

/**
 * Resolves the primary role for a user. A user may hold several roles;
 * navigation and the sidebar follow the most privileged one.
 */
export function primaryRole(roles: string[]): "Admin" | "Agent" | "Employee" | null {
  if (roles.includes("Admin")) return "Admin";
  if (roles.includes("Agent")) return "Agent";
  if (roles.includes("Employee")) return "Employee";
  return null;
}
