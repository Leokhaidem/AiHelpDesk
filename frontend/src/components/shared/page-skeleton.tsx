import { Skeleton } from "@/components/ui/skeleton";

/** Fallback for lazy-loaded route groups. */
export function PageSkeleton() {
  return (
    <div className="space-y-4" aria-busy="true" aria-label="Loading page">
      <Skeleton className="h-8 w-52" />
      <Skeleton className="h-4 w-72" />
      <Skeleton className="mt-6 h-64 w-full" />
    </div>
  );
}
