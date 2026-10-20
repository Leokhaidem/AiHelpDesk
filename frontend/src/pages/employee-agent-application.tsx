import { useState, type ReactNode } from "react";
import { BadgeCheck, Hourglass, XCircle } from "lucide-react";

import { ConfirmDialog } from "@/components/shared/confirm-dialog";
import { ErrorState } from "@/components/shared/error-state";
import { PageHeader } from "@/components/shared/page-header";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { formatDateTime } from "@/lib/dates";
import type { AgentApplicationResponse } from "@/features/applications/api";
import { AgentApplicationForm } from "@/features/applications/components/agent-application-form";
import { useMyApplication, useWithdrawApplication } from "@/features/applications/hooks";

function CategoryList({ application }: { application: AgentApplicationResponse }) {
  if (application.categories.length === 0) {
    return null;
  }
  return (
    <div className="mt-3 flex flex-wrap gap-1">
      {application.categories.map((category) => (
        <Badge key={category.id} variant="secondary" className="font-normal">
          {category.name}
        </Badge>
      ))}
    </div>
  );
}

function StatusCard({
  icon,
  title,
  description,
  children,
}: {
  icon: ReactNode;
  title: string;
  description: string;
  children?: ReactNode;
}) {
  return (
    <div className="max-w-2xl rounded-lg border bg-card p-6">
      <div className="flex items-start gap-3">
        <div className="mt-0.5 text-neutral-700">{icon}</div>
        <div className="flex-1">
          <h2 className="text-sm font-medium">{title}</h2>
          <p className="mt-1 text-sm text-muted-foreground">{description}</p>
          {children}
        </div>
      </div>
    </div>
  );
}

/**
 * P21 — the employee agent application state machine: never applied →
 * form; Pending → withdraw; Approved → success; Rejected / Withdrawn →
 * re-apply.
 */
export function AgentApplicationPage() {
  const application = useMyApplication();
  const withdraw = useWithdrawApplication();
  const [showForm, setShowForm] = useState(false);
  const [withdrawOpen, setWithdrawOpen] = useState(false);

  if (application.isPending) {
    return (
      <div>
        <PageHeader title="Agent application" />
        <div className="space-y-4" aria-busy="true" aria-label="Loading application">
          <Skeleton className="h-48 w-full" />
        </div>
      </div>
    );
  }

  if (application.isError) {
    return (
      <div>
        <PageHeader title="Agent application" />
        <ErrorState error={application.error} onRetry={() => void application.refetch()} />
      </div>
    );
  }

  const data = application.data;
  const canReapply = data !== null && (data.status === "Rejected" || data.status === "Withdrawn");

  if (data === null || (showForm && canReapply)) {
    return (
      <div>
        <PageHeader
          title={data === null ? "Become a support agent" : "Apply again"}
          description={
            data === null
              ? "Apply to join the support team and work tickets in your categories."
              : "Submit a new application — an admin will review it from scratch."
          }
        />
        <div className="max-w-2xl rounded-lg border bg-card p-6">
          <AgentApplicationForm />
        </div>
      </div>
    );
  }

  return (
    <div>
      <PageHeader
        title="Agent application"
        description="Track your application to join the support team."
      />

      {data.status === "Pending" ? (
        <StatusCard
          icon={<Hourglass className="size-6" aria-hidden="true" />}
          title="Application under review"
          description={`Submitted ${formatDateTime(data.createdAt)}. An admin will review it shortly — you'll see the decision here.`}
        >
          <CategoryList application={data} />
          <div className="mt-4">
            <Button variant="outline" size="sm" onClick={() => setWithdrawOpen(true)}>
              Withdraw application
            </Button>
          </div>
        </StatusCard>
      ) : null}

      {data.status === "Approved" ? (
        <StatusCard
          icon={<BadgeCheck className="size-6 text-emerald-600" aria-hidden="true" />}
          title="You're approved!"
          description={
            "You now have the Agent role. Sign out and sign in again to pick up the new role, then manage your availability from your profile."
          }
        >
          <p className="mt-3 text-xs text-muted-foreground">
            Reviewed {formatDateTime(data.reviewedAt)}
            {data.reviewNote ? ` · Note: ${data.reviewNote}` : ""}
          </p>
          <CategoryList application={data} />
        </StatusCard>
      ) : null}

      {data.status === "Rejected" ? (
        <StatusCard
          icon={<XCircle className="size-6 text-destructive" aria-hidden="true" />}
          title="Application rejected"
          description={
            data.reviewNote ??
            `Reviewed ${formatDateTime(data.reviewedAt)}. You can apply again if this changes.`
          }
        >
          <CategoryList application={data} />
          <div className="mt-4">
            <Button size="sm" onClick={() => setShowForm(true)}>
              Apply again
            </Button>
          </div>
        </StatusCard>
      ) : null}

      {data.status === "Withdrawn" ? (
        <StatusCard
          icon={<Hourglass className="size-6" aria-hidden="true" />}
          title="Application withdrawn"
          description="You withdrew this application. You can apply again whenever you're ready."
        >
          <div className="mt-4">
            <Button size="sm" onClick={() => setShowForm(true)}>
              Apply again
            </Button>
          </div>
        </StatusCard>
      ) : null}

      <ConfirmDialog
        open={withdrawOpen}
        onOpenChange={setWithdrawOpen}
        title="Withdraw your application?"
        description="You can apply again later if you change your mind."
        confirmLabel="Withdraw"
        destructive
        loading={withdraw.isPending}
        onConfirm={() => {
          withdraw.mutate(undefined, {
            onSettled: () => setWithdrawOpen(false),
          });
        }}
      />
    </div>
  );
}