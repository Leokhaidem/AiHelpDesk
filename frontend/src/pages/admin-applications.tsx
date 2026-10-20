import { useState } from "react";
import { ChevronLeft, ChevronRight, Inbox } from "lucide-react";

import { EmptyState } from "@/components/shared/empty-state";
import { ErrorState } from "@/components/shared/error-state";
import { PageHeader } from "@/components/shared/page-header";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Skeleton } from "@/components/ui/skeleton";
import { useAdminApplications } from "@/features/applications/hooks";
import type {
  AdminAgentApplicationResponse,
  ApplicationStatus,
} from "@/features/applications/api";
import { ApplicationReviewDialog } from "@/features/applications/components/application-review-dialog";
import { formatDateTime } from "@/lib/dates";
import { usePageParam, useUrlParam } from "@/lib/url-filters";

const PAGE_SIZE = 20;

const TABS: { value: ApplicationStatus; label: string }[] = [
  { value: "Pending", label: "Pending" },
  { value: "Approved", label: "Approved" },
  { value: "Rejected", label: "Rejected" },
  { value: "Withdrawn", label: "Withdrawn" },
];

/** P11 — agent application review: status tabs, table, review dialog. */
export function AdminApplicationsPage() {
  const [statusParam, setStatusParam] = useUrlParam("status");
  const [page, setPage] = usePageParam();
  const [selected, setSelected] = useState<AdminAgentApplicationResponse | null>(null);

  const status: ApplicationStatus = TABS.some((tab) => tab.value === statusParam)
    ? (statusParam as ApplicationStatus)
    : "Pending";

  const query = useAdminApplications(status, page, PAGE_SIZE);
  const result = query.data;
  const totalPages = result ? Math.max(1, Math.ceil(result.totalCount / PAGE_SIZE)) : 1;

  return (
    <div>
      <PageHeader
        title="Agent applications"
        description="Review employee applications for the agent role."
      />

      <Tabs value={status} onValueChange={(value) => setStatusParam(value)} className="mb-4">
        <TabsList>
          {TABS.map((tab) => (
            <TabsTrigger key={tab.value} value={tab.value}>
              {tab.label}
            </TabsTrigger>
          ))}
        </TabsList>
      </Tabs>

      {query.isError ? (
        <ErrorState error={query.error} onRetry={() => void query.refetch()} />
      ) : (
        <div className="rounded-xl border bg-card">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Applicant</TableHead>
                <TableHead>Categories</TableHead>
                <TableHead>Message</TableHead>
                <TableHead>Applied</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {query.isPending ? (
                Array.from({ length: 5 }, (_, rowIndex) => (
                  <TableRow key={rowIndex}>
                    {Array.from({ length: 4 }, (_, cellIndex) => (
                      <TableCell key={cellIndex}>
                        <Skeleton className="h-4 w-full max-w-40" />
                      </TableCell>
                    ))}
                  </TableRow>
                ))
              ) : (result?.items ?? []).length === 0 ? (
                <TableRow className="hover:bg-transparent">
                  <TableCell colSpan={4} className="p-0">
                    <EmptyState
                      icon={<Inbox className="size-10" aria-hidden="true" />}
                      title={`No ${status.toLowerCase()} applications`}
                      description={
                        status === "Pending"
                          ? "New applications from employees will appear here."
                          : "Applications with this status will appear here."
                      }
                    />
                  </TableCell>
                </TableRow>
              ) : (
                (result?.items ?? []).map((application) => (
                  <TableRow
                    key={application.id}
                    className="cursor-pointer"
                    role="button"
                    tabIndex={0}
                    onClick={() => setSelected(application)}
                    onKeyDown={(event) => {
                      if (event.key === "Enter" || event.key === " ") {
                        event.preventDefault();
                        setSelected(application);
                      }
                    }}
                  >
                    <TableCell>
                      <div className="font-medium">{application.applicantName}</div>
                      <div className="text-xs text-muted-foreground">
                        {application.applicantEmail}
                      </div>
                    </TableCell>
                    <TableCell>
                      <div className="flex flex-wrap gap-1">
                        {application.categories.slice(0, 3).map((category) => (
                          <Badge key={category.id} variant="secondary" className="font-normal">
                            {category.name}
                          </Badge>
                        ))}
                        {application.categories.length > 3 ? (
                          <span className="text-xs text-muted-foreground">
                            +{application.categories.length - 3}
                          </span>
                        ) : null}
                      </div>
                    </TableCell>
                    <TableCell className="max-w-64">
                      <p className="truncate text-sm text-muted-foreground">
                        {application.message ?? "—"}
                      </p>
                    </TableCell>
                    <TableCell className="whitespace-nowrap text-muted-foreground tabular-nums">
                      {formatDateTime(application.createdAt)}
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
          <div className="flex flex-col-reverse gap-2 border-t px-3 py-2.5 sm:flex-row sm:items-center sm:justify-between">
            <p className="text-xs text-muted-foreground">
              {result
                ? `${result.totalCount} application${result.totalCount === 1 ? "" : "s"}`
                : ""}
            </p>
            <div className="flex items-center gap-2">
              <Button
                variant="outline"
                size="icon-sm"
                aria-label="Previous page"
                disabled={page <= 1}
                onClick={() => setPage(page - 1)}
              >
                <ChevronLeft aria-hidden="true" />
              </Button>
              <span className="text-xs text-muted-foreground" aria-live="polite">
                Page {page} of {totalPages}
              </span>
              <Button
                variant="outline"
                size="icon-sm"
                aria-label="Next page"
                disabled={page >= totalPages}
                onClick={() => setPage(page + 1)}
              >
                <ChevronRight aria-hidden="true" />
              </Button>
            </div>
          </div>
        </div>
      )}

      <ApplicationReviewDialog
        application={selected}
        open={selected !== null}
        onOpenChange={(next) => !next && setSelected(null)}
      />
    </div>
  );
}