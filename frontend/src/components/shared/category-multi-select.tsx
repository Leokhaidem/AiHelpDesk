import * as React from "react";
import { Check, ChevronDown } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Command,
  CommandEmpty,
  CommandInput,
  CommandItem,
  CommandList,
} from "@/components/ui/command";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { cn } from "@/lib/utils";

export type CategoryOption = { id: number; name: string };

type CategoryMultiSelectProps = {
  value: number[];
  onValueChange: (ids: number[]) => void;
  options: CategoryOption[];
  placeholder?: string;
  disabled?: boolean;
  className?: string;
  id?: string;
  /** aria-labelledby pointing at the visible label. */
  "aria-labelledby"?: string;
};

/**
 * Multi-select built on Popover + Command. Used at registration, in the agent
 * profile, and in admin user/approval flows (plan section 8).
 */
export function CategoryMultiSelect({
  value,
  onValueChange,
  options,
  placeholder = "Select categories",
  disabled,
  className,
  id,
  "aria-labelledby": ariaLabelledBy,
}: CategoryMultiSelectProps) {
  const [open, setOpen] = React.useState(false);
  const selected = value
    .map((optionId) => options.find((option) => option.id === optionId))
    .filter((option): option is CategoryOption => option !== undefined);

  const toggle = (optionId: number) => {
    onValueChange(
      value.includes(optionId) ? value.filter((v) => v !== optionId) : [...value, optionId],
    );
  };

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button
          variant="outline"
          role="combobox"
          aria-expanded={open}
          aria-labelledby={ariaLabelledBy}
          aria-label={ariaLabelledBy ? undefined : "Select categories"}
          className={cn(
            "h-auto min-h-10 w-full justify-between gap-2 px-3 py-2 text-left font-normal",
            disabled && "pointer-events-none opacity-50",
            className,
          )}
          disabled={disabled}
          id={id}
        >
          {selected.length === 0 ? (
            <span className="truncate text-neutral-500">{placeholder}</span>
          ) : (
            <span className="flex flex-wrap gap-1">
              {selected.map((option) => (
                <Badge key={option.id} variant="secondary" className="font-normal">
                  {option.name}
                </Badge>
              ))}
            </span>
          )}
          <ChevronDown className="h-4 w-4 shrink-0 text-neutral-400" aria-hidden="true" />
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-(--radix-popover-trigger-width) p-0" align="start">
        <Command>
          <CommandInput placeholder="Search categories" />
          <CommandList>
            <CommandEmpty>No categories found.</CommandEmpty>
            {options.map((option) => (
              <CommandItem key={option.id} value={option.name} onSelect={() => toggle(option.id)}>
                <Check
                  className={cn("h-4 w-4", value.includes(option.id) ? "opacity-100" : "opacity-0")}
                  aria-hidden="true"
                />
                {option.name}
              </CommandItem>
            ))}
          </CommandList>
        </Command>
      </PopoverContent>
    </Popover>
  );
}
