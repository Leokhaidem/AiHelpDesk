import { useEffect, useState } from "react";
import { Loader2 } from "lucide-react";

import { CategoryMultiSelect } from "@/components/shared/category-multi-select";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { isApiError } from "@/lib/error-mapping";
import { useCategories } from "@/features/categories/hooks";

import { useSetUserRole } from "../hooks";

import type { AdminRole, AdminUser } from "../api";

type RoleChangeDialogProps = {
  user: AdminUser;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  /** Current Agent categories, used to pre-fill the category select. */
  currentCategories: number[];
};

/**
 * P8 — change a user's role between Employee and Agent. Promoting to Agent
 * optionally sets work categories (an agent with none receives no tickets
 * until they are added). Demoting an Agent triggers Flow I cleanup server-side.
 */
export function RoleChangeDialog({
  user,
  open,
  onOpenChange,
  currentCategories,
}: RoleChangeDialogProps) {
  const categories = useCategories(true);
  const setRole = useSetUserRole();

  const [role, setRoleValue] = useState<AdminRole>(
    user.roles.includes("Agent") ? "Agent" : "Employee",
  );
  const [categoryIds, setCategoryIds] = useState<number[]>([]);
  const [serverError, setServerError] = useState<string | null>(null);

  useEffect(() => {
    if (open) {
      setRoleValue(user.roles.includes("Agent") ? "Agent" : "Employee");
      setCategoryIds(currentCategories);
      setServerError(null);
    }
  }, [open, user, currentCategories]);

  const close = (next: boolean) => {
    if (!setRole.isPending) {
      onOpenChange(next);
    }
  };

  const submit = () => {
    setServerError(null);
    if (setRole.isPending) {
      return;
    }
    void setRole
      .mutateAsync({
        id: user.id,
        role,
        categoryIds: role === "Agent" && categoryIds.length > 0 ? categoryIds : undefined,
      })
      .then(() => close(false))
      .catch((error) => {
        if (isApiError(error)) {
          setServerError(error.detail);
        }
      });
  };

  return (
    <Dialog open={open} onOpenChange={close}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Change role</DialogTitle>
          <DialogDescription>
            Switch {user.fullName} between Employee and Agent.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          {serverError ? (
            <Alert variant="destructive">
              <AlertDescription>{serverError}</AlertDescription>
            </Alert>
          ) : null}
          {role === "Agent" ? (
            <Alert>
              <AlertDescription>
                Promoting to Agent re-runs the agent profile setup. Demoting an active Agent
                requeues their open Assigned tickets and flags InProgress ones for review.
              </AlertDescription>
            </Alert>
          ) : null}
          <div>
            <Label htmlFor="role-change-role">New role</Label>
            <Select value={role} onValueChange={(value) => setRoleValue(value as AdminRole)}>
              <SelectTrigger id="role-change-role" className="mt-1.5">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="Employee">Employee</SelectItem>
                <SelectItem value="Agent">Agent</SelectItem>
              </SelectContent>
            </Select>
          </div>
          {role === "Agent" ? (
            <div>
              <Label id="role-change-categories-label">Work categories (optional)</Label>
              <CategoryMultiSelect
                id="role-change-categories"
                aria-labelledby="role-change-categories-label"
                className="mt-1.5"
                value={categoryIds}
                onValueChange={setCategoryIds}
                options={(categories.data ?? []).map((c) => ({ id: c.id, name: c.name }))}
                placeholder="Select the categories this agent will work"
              />
              <p className="mt-1 text-xs text-muted-foreground">
                Leave empty to keep the current categories; an agent with no categories receives no
                tickets until some are set.
              </p>
            </div>
          ) : null}
        </div>

        <DialogFooter>
          <Button variant="ghost" disabled={setRole.isPending} onClick={() => close(false)}>
            Cancel
          </Button>
          <Button disabled={setRole.isPending} onClick={submit}>
            {setRole.isPending ? <Loader2 className="animate-spin" aria-hidden="true" /> : null}
            Change role
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}