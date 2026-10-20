import * as React from "react";
import type { ReactNode } from "react";
import { Loader2 } from "lucide-react";

import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";

type ConfirmDialogProps = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title: string;
  description?: ReactNode;
  /** Optional label/value rows — e.g. Flow I requeue / flag / auto-assign counts. */
  summary?: { label: string; value: string | number }[];
  confirmLabel?: string;
  cancelLabel?: string;
  /** Styles the confirm button destructive (irreversible actions, plan §11). */
  destructive?: boolean;
  /** True while the confirm mutation is in flight — locks the dialog. */
  loading?: boolean;
  onConfirm: () => void;
};

/**
 * Confirmation dialog for destructive / irreversible actions (plan §8/§11):
 * resolve, close, release, deactivation, role change, withdraw, override.
 */
export function ConfirmDialog({
  open,
  onOpenChange,
  title,
  description,
  summary,
  confirmLabel,
  cancelLabel,
  destructive,
  loading,
  onConfirm,
}: ConfirmDialogProps) {
  return (
    <Dialog
      open={open}
      onOpenChange={(next) => {
        if (!loading) {
          onOpenChange(next);
        }
      }}
    >
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
          {description ? <DialogDescription>{description}</DialogDescription> : null}
        </DialogHeader>
        {summary && summary.length > 0 ? (
          <dl className="grid grid-cols-[auto_1fr] gap-x-4 gap-y-1 rounded-md bg-muted/50 p-3 text-sm">
            {summary.map((row) => (
              <React.Fragment key={row.label}>
                <dt className="text-muted-foreground">{row.label}</dt>
                <dd className="text-right font-medium tabular-nums">{row.value}</dd>
              </React.Fragment>
            ))}
          </dl>
        ) : null}
        <DialogFooter>
          <Button variant="ghost" disabled={loading} onClick={() => onOpenChange(false)}>
            {cancelLabel ?? "Cancel"}
          </Button>
          <Button
            variant={destructive ? "destructive" : "default"}
            disabled={loading}
            onClick={onConfirm}
          >
            {loading ? <Loader2 className="animate-spin" aria-hidden="true" /> : null}
            {confirmLabel ?? "Confirm"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
