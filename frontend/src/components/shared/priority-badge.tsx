import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";

type PriorityBadgeProps = {
  name?: string;
  /** Level is known only from the priority lookup endpoints — ticket DTOs
   * carry the name only. Lower level = more urgent. */
  level?: number;
  className?: string;
};

const LEVEL_TONES: Record<number, string> = {
  1: "border-transparent bg-destructive/10 text-destructive dark:bg-destructive/20",
  2: "border-transparent bg-amber-100 text-amber-800 dark:bg-amber-500/15 dark:text-amber-300",
  3: "border-transparent bg-blue-100 text-blue-800 dark:bg-blue-500/15 dark:text-blue-300",
};

/**
 * Priority badge (plan §8): tone by level when known (1 = most urgent),
 * neutral tone otherwise.
 */
export function PriorityBadge({ name, level, className }: PriorityBadgeProps) {
  if (!name) {
    return <span className="text-sm text-muted-foreground">—</span>;
  }
  const tone = level !== undefined ? LEVEL_TONES[level] : undefined;
  return (
    <Badge variant="secondary" className={cn(tone, className)}>
      {name}
      {level !== undefined ? ` · ${level}` : null}
    </Badge>
  );
}
