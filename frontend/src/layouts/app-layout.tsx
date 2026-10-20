import { useState } from "react";
import {
  BarChart3,
  FolderOpen,
  Home,
  LayoutDashboard,
  LifeBuoy,
  ListOrdered,
  Menu,
  Plus,
  Ticket,
  UserCheck,
  UserCircle,
  Users,
  type LucideIcon,
} from "lucide-react";
import { NavLink, Outlet } from "react-router";

import { UserMenu } from "@/components/shared/user-menu";
import { Button } from "@/components/ui/button";
import { Sheet, SheetContent, SheetTrigger } from "@/components/ui/sheet";
import { primaryRole } from "@/layouts/guards";
import { cn } from "@/lib/utils";
import { useAuthStore } from "@/stores/auth-store";

type NavItem = {
  to: string;
  label: string;
  icon: LucideIcon;
  /** Match only the exact path (used for index-style entries). */
  end?: boolean;
};

const ADMIN_NAV: NavItem[] = [
  { to: "/admin", label: "Dashboard", icon: LayoutDashboard, end: true },
  { to: "/admin/tickets", label: "Tickets", icon: Ticket },
  { to: "/admin/users", label: "Users", icon: Users },
  { to: "/admin/applications", label: "Applications", icon: UserCheck },
  { to: "/admin/categories", label: "Categories", icon: FolderOpen },
  { to: "/admin/priorities", label: "Priorities", icon: ListOrdered },
  { to: "/admin/reports", label: "Reports", icon: BarChart3 },
];

const AGENT_NAV: NavItem[] = [
  { to: "/agent", label: "Dashboard", icon: LayoutDashboard, end: true },
  { to: "/agent/tickets", label: "Tickets", icon: Ticket },
  { to: "/agent/profile", label: "My profile", icon: UserCircle },
];

const EMPLOYEE_NAV: NavItem[] = [
  { to: "/employee", label: "Home", icon: Home, end: true },
  { to: "/employee/tickets", label: "My tickets", icon: Ticket },
  { to: "/employee/tickets/new", label: "New ticket", icon: Plus },
  { to: "/employee/agent-application", label: "Agent application", icon: UserCheck },
];

function navForRole(role: string): NavItem[] {
  if (role === "Admin") return ADMIN_NAV;
  if (role === "Agent") return AGENT_NAV;
  return EMPLOYEE_NAV;
}

function Brand() {
  return (
    <div className="flex h-14 shrink-0 items-center gap-2 border-b border-neutral-200 px-4">
      <LifeBuoy className="h-5 w-5 text-neutral-900" aria-hidden="true" />
      <span className="text-sm font-semibold tracking-tight text-neutral-900">AI Help Desk</span>
    </div>
  );
}

function SidebarNav({ items, onNavigate }: { items: NavItem[]; onNavigate?: () => void }) {
  return (
    <nav aria-label="Main navigation" className="flex flex-col gap-1">
      {items.map((item) => (
        <NavLink
          key={item.to}
          to={item.to}
          end={item.end}
          onClick={onNavigate}
          className={({ isActive }) =>
            cn(
              "flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors",
              isActive
                ? "bg-neutral-100 text-neutral-900"
                : "text-neutral-600 hover:bg-neutral-50 hover:text-neutral-900",
            )
          }
        >
          <item.icon className="h-4 w-4 shrink-0" aria-hidden="true" />
          {item.label}
        </NavLink>
      ))}
    </nav>
  );
}

/**
 * Authenticated app shell: fixed sidebar (desktop), sheet (mobile), top bar
 * with the user menu, and the routed page below (plan section 3).
 */
export function AppLayout() {
  const user = useAuthStore((state) => state.user);
  const role = primaryRole(user?.roles ?? []) ?? "Employee";
  const items = navForRole(role);
  const [mobileNavOpen, setMobileNavOpen] = useState(false);

  return (
    <div className="min-h-screen bg-neutral-50">
      <aside className="fixed inset-y-0 left-0 z-40 hidden w-60 flex-col border-r border-neutral-200 bg-white md:flex">
        <Brand />
        <div className="flex-1 overflow-y-auto p-3">
          <SidebarNav items={items} />
        </div>
      </aside>

      <Sheet open={mobileNavOpen} onOpenChange={setMobileNavOpen}>
        <SheetTrigger asChild>
          <Button
            variant="ghost"
            size="icon"
            className="md:hidden"
            aria-label="Open navigation menu"
          >
            <Menu className="h-5 w-5" aria-hidden="true" />
          </Button>
        </SheetTrigger>
        <SheetContent side="left" className="w-64 p-0">
          <div className="flex h-full flex-col">
            <Brand />
            <div className="flex-1 overflow-y-auto p-3">
              <SidebarNav items={items} onNavigate={() => setMobileNavOpen(false)} />
            </div>
          </div>
        </SheetContent>
      </Sheet>

      <div className="md:pl-60">
        <header className="sticky top-0 z-30 flex h-14 items-center justify-between gap-3 border-b border-neutral-200 bg-white px-4">
          <div className="hidden md:block" aria-hidden="true" />
          <UserMenu />
        </header>
        <main className="mx-auto w-full max-w-6xl p-4 md:p-6">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
