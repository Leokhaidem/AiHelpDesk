import type { ReactNode } from "react";
import { AlertTriangle, Loader2, Sparkles } from "lucide-react";

import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { cn } from "@/lib/utils";
import type { TicketStatus } from "@/types/api";

type AIBannerProps = {
  status: TicketStatus;
  aiSummary?: string | null;
  aiSuggestedResolution?: string | null;
  /** Rendered in the failed state — admin retry / override actions (M7). */
  actions?: ReactNode;
  className?: string;
};

/**
 * AI panel — always present on ticket detail pages (plan §8/§11):
 * analyzing spinner → summary + suggested resolution card → failed notice.
 */
export function AIBanner({
  status,
  aiSummary,
  aiSuggestedResolution,
  actions,
  className,
}: AIBannerProps) {
  const analyzing = status === "Created" || status === "AnalysisInProgress";
  const failed = status === "AnalysisFailed";

  if (analyzing) {
    return (
      <Alert className={className}>
        <Loader2 className="animate-spin" aria-hidden="true" />
        <AlertTitle>AI is analyzing your ticket…</AlertTitle>
        <AlertDescription>
          The AI classifies the category and priority and suggests a resolution. This usually takes a
          few seconds.
        </AlertDescription>
      </Alert>
    );
  }

  if (failed) {
    return (
      <div
        role="alert"
        className={cn(
          "rounded-lg border border-destructive/30 bg-destructive/5 p-4 text-sm",
          className,
        )}
      >
        <p className="flex items-center gap-2 font-medium text-destructive">
          <AlertTriangle aria-hidden="true" />
          AI analysis failed
        </p>
        <p className="mt-1 text-muted-foreground">
          The ticket could not be analyzed automatically and needs manual attention.
        </p>
        {actions ? <div className="mt-3 flex flex-wrap gap-2">{actions}</div> : null}
      </div>
    );
  }

  if (aiSummary || aiSuggestedResolution) {
    return (
      <section aria-label="AI analysis" className={cn("rounded-lg border bg-card p-4 text-sm", className)}>
        <h3 className="mb-2 flex items-center gap-1.5 text-sm font-medium">
          <Sparkles className="size-4 text-muted-foreground" aria-hidden="true" />
          AI analysis
        </h3>
        {aiSummary ? <p className="text-muted-foreground">{aiSummary}</p> : null}
        {aiSuggestedResolution ? (
          <div className={cn(aiSummary ? "mt-3" : undefined)}>
            <h4 className="mb-1 text-xs font-medium tracking-wide text-muted-foreground uppercase">
              Suggested resolution
            </h4>
            <p className="max-h-64 overflow-y-auto rounded-md bg-muted/50 p-3 whitespace-pre-wrap">
              {aiSuggestedResolution}
            </p>
          </div>
        ) : null}
      </section>
    );
  }

  return (
    <div
      className={cn(
        "rounded-lg border border-dashed p-4 text-sm text-muted-foreground",
        className,
      )}
    >
      No AI analysis is available for this ticket.
    </div>
  );
}
