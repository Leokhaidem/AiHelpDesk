import { useEffect, useState } from "react";
import { Mail, User } from "lucide-react";

import { CategoryMultiSelect } from "@/components/shared/category-multi-select";
import { ErrorState } from "@/components/shared/error-state";
import { PageHeader } from "@/components/shared/page-header";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Skeleton } from "@/components/ui/skeleton";
import { Switch } from "@/components/ui/switch";
import { useCategories } from "@/features/categories/hooks";
import {
  useAgentProfile,
  useSetAgentAvailability,
  useSetAgentCategories,
} from "@/features/agent-profile/hooks";

/** P16 — the agent's own work profile: availability, capacity, categories. */
export function AgentProfilePage() {
  const profile = useAgentProfile();
  const categories = useCategories(true);
  const setAvailability = useSetAgentAvailability();
  const setCategories = useSetAgentCategories();

  const data = profile.data;
  const [capacity, setCapacity] = useState("");
  const [selected, setSelected] = useState<number[] | null>(null);

  // Seed the editable fields once the profile loads.
  useEffect(() => {
    if (data) {
      setCapacity(String(data.maxOpenTickets));
      setSelected(data.categories.map((c) => c.id));
    }
  }, [data]);

  if (profile.isPending) {
    return (
      <div>
        <PageHeader title="My profile" />
        <div className="space-y-4" aria-busy="true" aria-label="Loading profile">
          <Skeleton className="h-32 w-full" />
          <Skeleton className="h-32 w-full" />
        </div>
      </div>
    );
  }

  if (profile.isError || !data) {
    return (
      <div>
        <PageHeader title="My profile" />
        <ErrorState error={profile.error} onRetry={() => void profile.refetch()} />
      </div>
    );
  }

  const capacityValue = Number.parseInt(capacity, 10);
  const capacityValid = Number.isFinite(capacityValue) && capacityValue >= 1;
  const capacityChanged = capacityValid && capacityValue !== data.maxOpenTickets;

  const selectedIds = selected ?? [];
  const categoriesChanged =
    selectedIds.length > 0 &&
    [...selectedIds].sort((a, b) => a - b).join(",") !==
      [...data.categories.map((c) => c.id)].sort((a, b) => a - b).join(",");

  return (
    <div>
      <PageHeader
        title="My profile"
        description="Control how the assignment algorithm works tickets to you."
      />
      <div className="grid items-start gap-4 lg:grid-cols-2">
        <section className="rounded-lg border bg-card p-4 lg:col-span-2">
          <h2 className="mb-2 text-sm font-medium text-muted-foreground">Profile</h2>
          <dl className="grid gap-2 sm:grid-cols-2">
            <div className="flex items-center gap-2">
              <User className="size-4 text-muted-foreground" aria-hidden="true" />
              <dt className="sr-only">Full name</dt>
              <dd className="text-sm font-medium">{data.fullName}</dd>
            </div>
            <div className="flex items-center gap-2">
              <Mail className="size-4 text-muted-foreground" aria-hidden="true" />
              <dt className="sr-only">Email</dt>
              <dd className="text-sm font-medium break-all">{data.email}</dd>
            </div>
          </dl>
        </section>

        <section className="rounded-lg border bg-card p-4">
          <h2 className="mb-2 text-sm font-medium text-muted-foreground">Availability</h2>
          <div className="flex items-center justify-between gap-3">
            <div>
              <p className="text-sm font-medium">{data.isAvailable ? "Available" : "Unavailable"}</p>
              <p className="mt-0.5 text-xs text-muted-foreground">
                While available, eligible tickets can be assigned to you. Changes re-run the
                assignment sweep.
              </p>
            </div>
            <Switch
              checked={data.isAvailable}
              disabled={setAvailability.isPending}
              onCheckedChange={(next) =>
                setAvailability.mutate({ isAvailable: next, kind: "availability" })
              }
              aria-label="Available to receive tickets"
            />
          </div>
          <p className="mt-3 text-xs text-muted-foreground">
            Open tickets: <span className="font-medium tabular-nums">{data.openTicketCount}</span>{" "}
            / <span className="font-medium tabular-nums">{data.maxOpenTickets}</span>
          </p>
        </section>

        <section className="rounded-lg border bg-card p-4">
          <h2 className="mb-2 text-sm font-medium text-muted-foreground">Capacity</h2>
          <div className="flex items-end gap-2">
            <div className="flex-1">
              <Label htmlFor="capacity">Max open tickets</Label>
              <Input
                id="capacity"
                type="number"
                min={1}
                className="mt-1.5"
                value={capacity}
                onChange={(event) => setCapacity(event.target.value)}
                aria-invalid={!capacityValid ? true : undefined}
              />
              {!capacityValid ? (
                <p className="mt-1 text-sm text-red-600" role="alert">
                  Enter a number of at least 1.
                </p>
              ) : null}
            </div>
            <Button
              disabled={!capacityChanged || setAvailability.isPending}
              onClick={() =>
                setAvailability.mutate({
                  isAvailable: data.isAvailable,
                  maxOpenTickets: capacityValue,
                  kind: "capacity",
                })
              }
            >
              Save
            </Button>
          </div>
        </section>

        <section className="rounded-lg border bg-card p-4 lg:col-span-2">
          <h2 className="mb-2 text-sm font-medium text-muted-foreground">Work categories</h2>
          <p className="mb-3 text-xs text-muted-foreground">
            You only receive tickets in these categories. Keep at least one selected.
          </p>
          <div className="flex items-start gap-2">
            <div className="max-w-md flex-1">
              <CategoryMultiSelect
                value={selectedIds}
                onValueChange={setSelected}
                options={(categories.data ?? []).map((c) => ({ id: c.id, name: c.name }))}
                placeholder="Select the categories you work on"
                disabled={setCategories.isPending}
                id="agent-profile-categories"
              />
            </div>
            <Button
              disabled={!categoriesChanged || setCategories.isPending}
              onClick={() => setCategories.mutate(selectedIds)}
            >
              Save
            </Button>
          </div>
        </section>
      </div>
    </div>
  );
}