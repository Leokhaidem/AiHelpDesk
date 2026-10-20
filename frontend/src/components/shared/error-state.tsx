import { AlertTriangle, RefreshCw } from "lucide-react";

import { Button } from "@/components/ui/button";
import { isApiError } from "@/lib/error-mapping";
import { cn } from "@/lib/utils";

type ErrorStateProps = {
  error: unknown;
  /** Retry action — re-invokes the failed query. Omit for fatal errors. */
  onRetry?: () => void;
  className?: string;
};

/**
 * Error state with the normalized ApiError detail (plan §8) + retry button.
 */
export function ErrorState({ error, onRetry, className }: ErrorStateProps) {
  const detail = isApiError(error) ? error.detail : "Something went wrong. Please try again.";
  return (
    <div role="alert" className={cn("flex flex-col items-center gap-2 px-6 py-16 text-center", className)}>
      <AlertTriangle className="size-8 text-destructive" aria-hidden="true" />
      <p className="text-sm font-medium text-foreground">Something went wrong</p>
      <p className="max-w-md text-sm text-muted-foreground">{detail}</p>
      {onRetry ? (
        <Button variant="outline" size="sm" className="mt-2" onClick={onRetry}>
          <RefreshCw aria-hidden="true" />
          Try again
        </Button>
      ) : null}
    </div>
  );
}
