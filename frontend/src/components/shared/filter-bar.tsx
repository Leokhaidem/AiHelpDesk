import type { ReactNode } from "react";
import { X } from "lucide-react";

import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

type FilterBarProps = {
  /** Filter controls — selects, date inputs, etc. (plan §8). */
  children: ReactNode;
  /** Shows the "Clear filters" button when true. */
  hasActiveFilters?: boolean;
  onClearFilters?: () => void;
  className?: string;
};

/**
 * Filter controls row (plan §8). Controls are synced to URL search params
 * by the page (lib/url-filters.ts) so links are shareable.
 */
export function FilterBar({ children, hasActiveFilters, onClearFilters, className }: FilterBarProps) {
  return (
    <div
      className={cn(
        "mb-4 flex flex-wrap items-end gap-3 rounded-lg border bg-muted/30 p-3",
        className,
      )}
    >
      {children}
      {hasActiveFilters && onClearFilters ? (
        <Button
          variant="ghost"
          size="sm"
          className="ml-auto text-muted-foreground"
          onClick={onClearFilters}
        >
          <X aria-hidden="true" />
          Clear filters
        </Button>
      ) : null}
    </div>
  );
}
