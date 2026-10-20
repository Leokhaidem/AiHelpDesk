import { Loader2 } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";
import { TICKET_STATUS_LABELS, type TicketStatus } from "@/types/api";

type StatusTone = { className: string; spinner?: boolean };

const TONES: Record<TicketStatus, StatusTone> = {
  Created: { className: "border-transparent bg-secondary text-secondary-foreground" },
  AnalysisInProgress: {
    className: "border-transparent bg-blue-100 text-blue-800 dark:bg-blue-500/15 dark:text-blue-300",
    spinner: true,
  },
  Assigned: { className: "border-border bg-background text-foreground" },
  InProgress: {
    className: "border-transparent bg-blue-100 text-blue-800 dark:bg-blue-500/15 dark:text-blue-300",
  },
  Resolved: {
    className: "border-transparent bg-emerald-100 text-emerald-800 dark:bg-emerald-500/15 dark:text-emerald-300",
  },
  Closed: { className: "border-transparent bg-muted text-muted-foreground" },
  AnalysisFailed: {
    className: "border-transparent bg-destructive/10 text-destructive dark:bg-destructive/20",
  },
  AwaitingAssignment: {
    className: "border-transparent bg-amber-100 text-amber-800 dark:bg-amber-500/15 dark:text-amber-300",
  },
};

/**
 * Text + color badge for all 8 TicketStatus values (plan §8) — never
 * color-only, for accessibility.
 */
export function StatusBadge({ status, className }: { status: TicketStatus; className?: string }) {
  const tone = TONES[status];
  return (
    <Badge variant="secondary" className={cn(tone.className, className)}>
      {tone.spinner ? <Loader2 className="animate-spin" aria-hidden="true" /> : null}
      {TICKET_STATUS_LABELS[status]}
    </Badge>
  );
}
