import { useEffect, useState } from "react";
import { Loader2 } from "lucide-react";

import { FieldError } from "@/components/shared/field-error";
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
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { isApiError } from "@/lib/error-mapping";

import { useCreatePriority, useUpdatePriority } from "../hooks";

import type { PriorityResponse } from "../api";

type PriorityDialogProps = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  /** When provided the dialog edits this priority; otherwise it creates one. */
  priority?: PriorityResponse;
};

/** P10 — create / edit a priority (name + level; lower level = more urgent). */
export function PriorityDialog({ open, onOpenChange, priority }: PriorityDialogProps) {
  const createPriority = useCreatePriority();
  const updatePriority = useUpdatePriority();
  const isEdit = priority !== undefined;

  const [name, setName] = useState("");
  const [level, setLevel] = useState("");
  const [touched, setTouched] = useState(false);
  const [serverError, setServerError] = useState<string | null>(null);

  useEffect(() => {
    if (open) {
      setName(priority?.name ?? "");
      setLevel(priority ? String(priority.level) : "");
      setTouched(false);
      setServerError(null);
    }
  }, [open, priority]);

  const nameError = name.trim().length === 0 ? "Name is required." : undefined;
  const levelValue = Number.parseInt(level, 10);
  const levelError =
    !Number.isFinite(levelValue) || levelValue < 1 ? "Enter a level of 1 or higher." : undefined;
  const pending = createPriority.isPending || updatePriority.isPending;

  const close = (next: boolean) => {
    if (!pending) {
      onOpenChange(next);
    }
  };

  const submit = () => {
    setTouched(true);
    setServerError(null);
    if (nameError || levelError || pending) {
      return;
    }
    const run =
      isEdit && priority
        ? updatePriority.mutateAsync({ id: priority.id, name: name.trim(), level: levelValue })
        : createPriority.mutateAsync({ name: name.trim(), level: levelValue });
    void run.then(() => close(false)).catch((error) => {
      if (isApiError(error)) {
        setServerError(error.detail);
      }
    });
  };

  return (
    <Dialog open={open} onOpenChange={close}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{isEdit ? `Edit priority ${priority?.id}` : "New priority"}</DialogTitle>
          <DialogDescription>
            Lower levels are more urgent (1 = most urgent). Levels must be unique.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          {serverError ? (
            <Alert variant="destructive">
              <AlertDescription>{serverError}</AlertDescription>
            </Alert>
          ) : null}
          <div>
            <Label htmlFor="priority-name">Name</Label>
            <Input
              id="priority-name"
              className="mt-1.5"
              maxLength={100}
              value={name}
              onChange={(event) => setName(event.target.value)}
              onBlur={() => setTouched(true)}
              aria-invalid={nameError ? true : undefined}
            />
            <FieldError message={touched ? nameError : undefined} />
          </div>
          <div>
            <Label htmlFor="priority-level">Level</Label>
            <Input
              id="priority-level"
              className="mt-1.5 w-24"
              type="number"
              min={1}
              value={level}
              onChange={(event) => setLevel(event.target.value)}
              onBlur={() => setTouched(true)}
              aria-invalid={levelError ? true : undefined}
            />
            <FieldError message={touched ? levelError : undefined} />
          </div>
        </div>

        <DialogFooter>
          <Button variant="ghost" disabled={pending} onClick={() => close(false)}>
            Cancel
          </Button>
          <Button disabled={pending} onClick={submit}>
            {pending ? <Loader2 className="animate-spin" aria-hidden="true" /> : null}
            {isEdit ? "Save changes" : "Create priority"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}