import { Link } from "react-router";
import { ArrowLeft } from "lucide-react";

import { PageHeader } from "@/components/shared/page-header";
import { NewTicketForm } from "@/features/tickets/components/new-ticket-form";

/** P18 — create a ticket; redirects to the detail page where the AI runs. */
export function EmployeeNewTicketPage() {
  return (
    <div>
      <div className="mb-3">
        <Link
          to="/employee/tickets"
          className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
        >
          <ArrowLeft className="size-4" aria-hidden="true" />
          Back to my tickets
        </Link>
      </div>
      <PageHeader
        title="New ticket"
        description="Describe your issue — the AI classifies it and a support agent will follow up."
      />
      <div className="max-w-2xl rounded-lg border bg-card p-6">
        <NewTicketForm />
      </div>
    </div>
  );
}