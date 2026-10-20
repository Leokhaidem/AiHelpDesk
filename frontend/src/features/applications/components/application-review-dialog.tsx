import { useEffect, useState } from "react";
import { Check, Loader2, X } from "lucide-react";

import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
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
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Textarea } from "@/components/ui/textarea";
import { CategoryMultiSelect } from "@/components/shared/category-multi-select";
import { useCategories } from "@/features/categories/hooks";
import { useApproveApplication, useRejectApplication } from "@/features/applications/hooks";
import type { AdminAgentApplicationResponse } from "@/features/applications/api";
import { formatDateTime } from "@/lib/dates";
import { isApiError } from "@/lib/error-mapping";

type ApplicationReviewDialogProps = {
  application: AdminAgentApplicationResponse | null;
  open: boolean;
  onOpenChange: (open: boolean) => void;
};

/**
 * P11 — review one application: full details plus approve (optional
 * category override + note) or reject (note). A 409 (no longer pending)
 * is shown inline; the list refetches via the mutation hooks.
 */
export function ApplicationReviewDialog({
  application,
  open,
  onOpenChange,
}: ApplicationReviewDialogProps) {
  const [mode, setMode] = useState<"approve" | "reject">("approve");
  const [categoryIds, setCategoryIds] = useState<number[]>([]);
  const [note, setNote] = useState("");

  const categories = useCategories(true);
  const approve = useApproveApplication();
  const reject = useRejectApplication();

  // Re-seed the form each time the dialog opens for an application.
  useEffect(() => {
    if (open && application) {
      setMode("approve");
      setCategoryIds(application.categories.map((category) => category.id));
      setNote("");
    }
  }, [open, application]);

  const busy = approve.isPending || reject.isPending;
  const isPendingApplication = application?.status === "Pending";
  const hasConflict =
    (isApiError(approve.error) && approve.error.status === 409) ||
    (isApiError(reject.error) && reject.error.status === 409);

  const confirm = () => {
    if (!application) return;
    if (mode === "approve") {
      approve.mutate({ id: application.id, categoryIds, note }, {
        onSuccess: () => onOpenChange(false),
      });
    } else {
      reject.mutate({ id: application.id, note }, {
        onSuccess: () => onOpenChange(false),
      });
    }
  };

  return (
    <Dialog open={open} onOpenChange={(next) => !busy && onOpenChange(next)}>
      <DialogContent className="max-h-[85vh] overflow-y-auto sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>Review application</DialogTitle>
          <DialogDescription>
            {application ? `${application.applicantName} · ${application.applicantEmail}` : ""}
          </DialogDescription>
        </DialogHeader>

        {application ? (
          <div className="space-y-4">
            <dl className="grid grid-cols-[auto_1fr] gap-x-4 gap-y-1 rounded-md bg-muted/50 p-3 text-sm">
              <dt className="text-muted-foreground">Status</dt>
              <dd className="text-right">{application.status}</dd>
              <dt className="text-muted-foreground">Applied</dt>
              <dd className="text-right tabular-nums">
                {formatDateTime(application.createdAt)}
              </dd>
              {application.reviewedAt ? (
                <>
                  <dt className="text-muted-foreground">Reviewed</dt>
                  <dd className="text-right tabular-nums">
                    {formatDateTime(application.reviewedAt)}
                  </dd>
                </>
              ) : null}
            </dl>

            {application.message ? (
              <div>
                <h3 className="text-xs font-medium tracking-wide text-muted-foreground uppercase">
                  Message
                </h3>
                <p className="mt-1 rounded-md bg-muted/40 p-3 text-sm whitespace-pre-wrap">
                  {application.message}
                </p>
              </div>
            ) : null}

            <div>
              <h3 className="text-xs font-medium tracking-wide text-muted-foreground uppercase">
                Requested categories
              </h3>
              <div className="mt-1.5 flex flex-wrap gap-1">
                {application.categories.map((category) => (
                  <Badge key={category.id} variant="secondary" className="font-normal">
                    {category.name}
                  </Badge>
                ))}
              </div>
            </div>

            {application.reviewNote ? (
              <div>
                <h3 className="text-xs font-medium tracking-wide text-muted-foreground uppercase">
                  Review note
                </h3>
                <p className="mt-1 rounded-md bg-muted/40 p-3 text-sm whitespace-pre-wrap">
                  {application.reviewNote}
                </p>
              </div>
            ) : null}

            {hasConflict && !busy ? (
              <Alert variant="destructive">
                <AlertTitle>No longer pending</AlertTitle>
                <AlertDescription>
                  This application has already been reviewed or withdrawn. The list has been
                  refreshed — pick another application.
                </AlertDescription>
              </Alert>
            ) : null}
            {isPendingApplication && !hasConflict ? (
              <div className="space-y-3">
                <Tabs
                  value={mode}
                  onValueChange={(value) => setMode(value as "approve" | "reject")}
                  className="w-full"
                >
                  <TabsList className="grid w-full grid-cols-2">
                    <TabsTrigger value="approve">Approve</TabsTrigger>
                    <TabsTrigger value="reject">Reject</TabsTrigger>
                  </TabsList>
                </Tabs>
                {mode === "approve" ? (
                  <div>
                    <Label id="review-categories-label">Agent categories</Label>
                    <p className="mt-0.5 text-xs text-muted-foreground">
                      Pre-filled with the requested categories — leave empty to keep them.
                    </p>
                    <CategoryMultiSelect
                      className="mt-1.5"
                      aria-labelledby="review-categories-label"
                      value={categoryIds}
                      onValueChange={setCategoryIds}
                      options={(categories.data ?? []).map((category) => ({
                        id: category.id,
                        name: category.name,
                      }))}
                      disabled={busy}
                    />
                  </div>
                ) : null}
                <div>
                  <Label htmlFor="review-note">Note (optional)</Label>
                  <Textarea
                    id="review-note"
                    rows={3}
                    className="mt-1.5"
                    placeholder={
                      mode === "approve"
                        ? "e.g. Welcome aboard — we adjusted your categories above."
                        : "e.g. We need more agents for other categories right now."
                    }
                    value={note}
                    onChange={(event) => setNote(event.target.value)}
                    disabled={busy}
                  />
                </div>
              </div>
            ) : null}
          </div>
        ) : null}

        <DialogFooter>
          <Button variant="ghost" disabled={busy} onClick={() => onOpenChange(false)}>
            Close
          </Button>
          {isPendingApplication && !hasConflict ? (
            <Button
              variant={mode === "reject" ? "destructive" : "default"}
              disabled={busy}
              onClick={confirm}
            >
              {mode === "approve" ? (
                approve.isPending ? (
                  <Loader2 className="animate-spin" aria-hidden="true" />
                ) : (
                  <Check aria-hidden="true" />
                )
              ) : reject.isPending ? (
                <Loader2 className="animate-spin" aria-hidden="true" />
              ) : (
                <X aria-hidden="true" />
              )}
              {mode === "approve" ? "Approve applicant" : "Reject application"}
            </Button>
          ) : null}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}