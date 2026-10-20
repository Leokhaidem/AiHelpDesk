import { Route, Routes } from "react-router";

import { AppLayout } from "@/layouts/app-layout";
import { ProtectedRoute, RequireRole } from "@/layouts/guards";
import { AdminApplicationsPage } from "@/pages/admin-applications";
import { AdminCategoriesPage } from "@/pages/admin-categories";
import { AdminHomePage } from "@/pages/admin-home";
import { AdminPrioritiesPage } from "@/pages/admin-priorities";
import { AdminReportsPage } from "@/pages/admin-reports";
import { AdminTicketDetailPage } from "@/pages/admin-ticket-detail";
import { AdminTicketListPage } from "@/pages/admin-ticket-list";
import { AdminUserDetailPage } from "@/pages/admin-user-detail";
import { AdminUsersPage } from "@/pages/admin-users";
import { AgentHomePage } from "@/pages/agent-home";
import { AgentProfilePage } from "@/pages/agent-profile";
import { AgentTicketDetailPage } from "@/pages/agent-ticket-detail";
import { AgentTicketListPage } from "@/pages/agent-ticket-list";
import { AgentApplicationPage } from "@/pages/employee-agent-application";
import { EmployeeHomePage } from "@/pages/employee-home";
import { EmployeeNewTicketPage } from "@/pages/employee-new-ticket";
import { EmployeeTicketDetailPage } from "@/pages/employee-ticket-detail";
import { EmployeeTicketListPage } from "@/pages/employee-ticket-list";
import { LoginPage } from "@/pages/login-page";
import { NotFoundPage } from "@/pages/not-found-page";
import { RegisterPage } from "@/pages/register-page";
import { RoleRedirect } from "@/pages/role-home";
import { UnauthorizedPage } from "@/pages/unauthorized-page";

export default function App() {
  return (
    <Routes>
      {/* Public */}
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
      <Route path="/unauthorized" element={<UnauthorizedPage />} />

      {/* Session required */}
      <Route
        path="/"
        element={
          <ProtectedRoute>
            <RoleRedirect />
          </ProtectedRoute>
        }
      />

      {/* Authenticated shell */}
      <Route
        element={
          <ProtectedRoute>
            <AppLayout />
          </ProtectedRoute>
        }
      >
        <Route
          path="/admin"
          element={
            <RequireRole roles={["Admin"]}>
              <AdminHomePage />
            </RequireRole>
          }
        />
        <Route
          path="/agent"
          element={
            <RequireRole roles={["Agent"]}>
              <AgentHomePage />
            </RequireRole>
          }
        />
        <Route
          path="/employee"
          element={
            <RequireRole roles={["Employee"]}>
              <EmployeeHomePage />
            </RequireRole>
          }
        />
        {/* Admin tickets (M3 list + M7 ops) */}
        <Route
          path="/admin/tickets"
          element={
            <RequireRole roles={["Admin"]}>
              <AdminTicketListPage />
            </RequireRole>
          }
        />
        <Route
          path="/admin/tickets/:id"
          element={
            <RequireRole roles={["Admin"]}>
              <AdminTicketDetailPage />
            </RequireRole>
          }
        />
        {/* Admin users + lookups (M6) */}
        <Route
          path="/admin/users"
          element={
            <RequireRole roles={["Admin"]}>
              <AdminUsersPage />
            </RequireRole>
          }
        />
        <Route
          path="/admin/users/:id"
          element={
            <RequireRole roles={["Admin"]}>
              <AdminUserDetailPage />
            </RequireRole>
          }
        />
        <Route
          path="/admin/categories"
          element={
            <RequireRole roles={["Admin"]}>
              <AdminCategoriesPage />
            </RequireRole>
          }
        />
        <Route
          path="/admin/priorities"
          element={
            <RequireRole roles={["Admin"]}>
              <AdminPrioritiesPage />
            </RequireRole>
          }
        />
        {/* Admin ops (M7) */}
        <Route
          path="/admin/applications"
          element={
            <RequireRole roles={["Admin"]}>
              <AdminApplicationsPage />
            </RequireRole>
          }
        />
        <Route
          path="/admin/reports"
          element={
            <RequireRole roles={["Admin"]}>
              <AdminReportsPage />
            </RequireRole>
          }
        />
        {/* Agent tickets (read-only in M3; actions in M4) */}
        <Route
          path="/agent/tickets"
          element={
            <RequireRole roles={["Agent"]}>
              <AgentTicketListPage />
            </RequireRole>
          }
        />
        <Route
          path="/agent/tickets/:id"
          element={
            <RequireRole roles={["Agent"]}>
              <AgentTicketDetailPage />
            </RequireRole>
          }
        />
        <Route
          path="/agent/profile"
          element={
            <RequireRole roles={["Agent"]}>
              <AgentProfilePage />
            </RequireRole>
          }
        />
        {/* Employee tickets (static /new wins over :id) */}
        <Route
          path="/employee/tickets"
          element={
            <RequireRole roles={["Employee"]}>
              <EmployeeTicketListPage />
            </RequireRole>
          }
        />
        <Route
          path="/employee/tickets/new"
          element={
            <RequireRole roles={["Employee"]}>
              <EmployeeNewTicketPage />
            </RequireRole>
          }
        />
        <Route
          path="/employee/tickets/:id"
          element={
            <RequireRole roles={["Employee"]}>
              <EmployeeTicketDetailPage />
            </RequireRole>
          }
        />
        <Route
          path="/employee/agent-application"
          element={
            <RequireRole roles={["Employee"]}>
              <AgentApplicationPage />
            </RequireRole>
          }
        />
      </Route>

      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  );
}
