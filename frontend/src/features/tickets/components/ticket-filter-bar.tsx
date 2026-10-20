import { FilterBar } from "@/components/shared/filter-bar";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Switch } from "@/components/ui/switch";
import { usePublicCategories } from "@/features/categories/hooks";
import { usePublicPriorities } from "@/features/priorities/hooks";
import { useAgents } from "@/features/users/hooks";
import { toTicketStatus } from "@/features/tickets/api";
import { TICKET_STATUS_LABELS, TICKET_STATUSES } from "@/types/api";

import type { TicketScope } from "../hooks";

type TicketFilterBarProps = {
  scope: TicketScope;
  /** Raw URL search param values (strings); the page owns the URL state. */
  status?: string;
  categoryId?: string;
  priorityId?: string;
  assignedAgentId?: string;
  agentInactive?: boolean;
  fromDate?: string;
  toDate?: string;
  onStatusChange: (value: string | undefined) => void;
  onCategoryIdChange: (value: string | undefined) => void;
  onPriorityIdChange: (value: string | undefined) => void;
  onAssignedAgentIdChange?: (value: string | undefined) => void;
  onAgentInactiveChange?: (value: boolean) => void;
  onFromDateChange: (value: string | undefined) => void;
  onToDateChange: (value: string | undefined) => void;
  hasActiveFilters: boolean;
  onClearFilters: () => void;
};

/**
 * Shared ticket list filters (plan §3/§8) — status, category, priority,
 * created-from/to; agent + deactivated-agent toggle for the admin scope.
 */
export function TicketFilterBar({
  scope,
  status,
  categoryId,
  priorityId,
  assignedAgentId,
  agentInactive,
  fromDate,
  toDate,
  onStatusChange,
  onCategoryIdChange,
  onPriorityIdChange,
  onAssignedAgentIdChange,
  onAgentInactiveChange,
  onFromDateChange,
  onToDateChange,
  hasActiveFilters,
  onClearFilters,
}: TicketFilterBarProps) {
  const statusFilter = toTicketStatus(status);
  const categories = usePublicCategories();
  const priorities = usePublicPriorities();
  const agents = useAgents(scope === "admin");

  return (
    <FilterBar hasActiveFilters={hasActiveFilters} onClearFilters={onClearFilters}>
      <div className="flex flex-col gap-1">
        <Label htmlFor="filter-status">Status</Label>
        <Select
          value={statusFilter ?? "all"}
          onValueChange={(value) => onStatusChange(value === "all" ? undefined : value)}
        >
          <SelectTrigger id="filter-status" className="w-48">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All statuses</SelectItem>
            {TICKET_STATUSES.map((s) => (
              <SelectItem key={s} value={s}>
                {TICKET_STATUS_LABELS[s]}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>
      <div className="flex flex-col gap-1">
        <Label htmlFor="filter-category">Category</Label>
        <Select
          value={categoryId ?? "all"}
          onValueChange={(value) => onCategoryIdChange(value === "all" ? undefined : value)}
        >
          <SelectTrigger id="filter-category" className="w-44">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All categories</SelectItem>
            {(categories.data ?? []).map((category) => (
              <SelectItem key={category.id} value={String(category.id)}>
                {category.name}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>
      <div className="flex flex-col gap-1">
        <Label htmlFor="filter-priority">Priority</Label>
        <Select
          value={priorityId ?? "all"}
          onValueChange={(value) => onPriorityIdChange(value === "all" ? undefined : value)}
        >
          <SelectTrigger id="filter-priority" className="w-40">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All priorities</SelectItem>
            {(priorities.data ?? []).map((priority) => (
              <SelectItem key={priority.id} value={String(priority.id)}>
                {priority.name}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>
      {scope === "admin" ? (
        <div className="flex flex-col gap-1">
          <Label htmlFor="filter-agent">Agent</Label>
          <Select
            value={assignedAgentId ?? "all"}
            onValueChange={(value) =>
              onAssignedAgentIdChange?.(value === "all" ? undefined : value)
            }
          >
            <SelectTrigger id="filter-agent" className="w-44">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All agents</SelectItem>
              {(agents.data ?? []).map((agent) => (
                <SelectItem key={agent.id} value={agent.id}>
                  {agent.fullName}
                  {agent.isActive ? "" : " (deactivated)"}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      ) : null}
      <div className="flex flex-col gap-1">
        <Label htmlFor="filter-from">Created from</Label>
        <Input
          id="filter-from"
          type="date"
          className="w-40"
          value={fromDate ?? ""}
          onChange={(event) => onFromDateChange(event.target.value || undefined)}
        />
      </div>
      <div className="flex flex-col gap-1">
        <Label htmlFor="filter-to">Created to</Label>
        <Input
          id="filter-to"
          type="date"
          className="w-40"
          value={toDate ?? ""}
          onChange={(event) => onToDateChange(event.target.value || undefined)}
        />
      </div>
      {scope === "admin" && onAgentInactiveChange ? (
        <div className="flex items-center gap-2 pb-2">
          <Switch
            id="filter-agent-inactive"
            checked={agentInactive ?? false}
            onCheckedChange={(checked) => onAgentInactiveChange?.(checked)}
          />
          <Label htmlFor="filter-agent-inactive">Deactivated agents only</Label>
        </div>
      ) : null}
    </FilterBar>
  );
}
