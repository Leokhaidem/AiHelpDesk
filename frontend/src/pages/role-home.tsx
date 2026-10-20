import { Navigate } from "react-router";

import { primaryRole } from "@/layouts/guards";
import { useAuthStore } from "@/stores/auth-store";

const ROLE_HOME: Record<"Admin" | "Agent" | "Employee", string> = {
  Admin: "/admin",
  Agent: "/agent",
  Employee: "/employee",
};

/** P3 — sends a signed-in user to the home of their most privileged role. */
export function RoleRedirect() {
  const user = useAuthStore((state) => state.user);
  const role = primaryRole(user?.roles ?? []);

  if (!role) {
    return <Navigate to="/unauthorized" replace />;
  }
  return <Navigate to={ROLE_HOME[role]} replace />;
}
