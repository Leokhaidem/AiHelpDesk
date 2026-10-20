import { useEffect } from "react";
import { LogOut } from "lucide-react";
import { useNavigate } from "react-router";
import { useQueryClient } from "@tanstack/react-query";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { primaryRole } from "@/layouts/guards";
import { useAuthStore } from "@/stores/auth-store";

/**
 * Top-bar user menu. Signs out locally (the backend is stateless) and clears
 * the in-memory query cache so no server data survives the session (plan
 * section 5).
 */
export function UserMenu() {
  const user = useAuthStore((state) => state.user);
  const logout = useAuthStore((state) => state.logout);
  const navigate = useNavigate();
  const client = useQueryClient();

  const role = primaryRole(user?.roles ?? []);

  // Flush the cache right after the store resets; the navigation happens in
  // the same tick from the click handler.
  useEffect(() => {
    return () => {
      client.clear();
    };
  }, [client]);

  const handleSignOut = () => {
    logout();
    void client.clear();
    navigate("/login", { replace: true });
  };

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          variant="ghost"
          className="h-9 gap-2 px-2 text-sm font-normal"
          aria-label="Account menu"
        >
          <span className="max-w-40 truncate text-neutral-700">{user?.email ?? "Unknown"}</span>
          {role ? (
            <Badge variant="secondary" className="text-xs font-medium">
              {role}
            </Badge>
          ) : null}
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-56">
        <DropdownMenuLabel className="font-normal">
          <div className="truncate text-sm font-medium text-neutral-900">
            {user?.email ?? "Unknown"}
          </div>
          <div className="mt-0.5 flex flex-wrap gap-1">
            {(user?.roles ?? []).map((r) => (
              <Badge key={r} variant="outline" className="text-xs font-normal">
                {r}
              </Badge>
            ))}
          </div>
        </DropdownMenuLabel>
        <DropdownMenuSeparator />
        <DropdownMenuItem onClick={handleSignOut} className="cursor-pointer">
          <LogOut className="h-4 w-4" aria-hidden="true" />
          Sign out
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
