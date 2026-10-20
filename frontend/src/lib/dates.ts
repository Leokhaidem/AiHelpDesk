import { format } from "date-fns";

const DATE_TIME_PATTERN = "MMM d, yyyy HH:mm";

/**
 * Formats a server timestamp in the user's local time zone.
 * Plan §11: `MMM d, yyyy HH:mm`. Server timestamps are UTC (detail pages
 * note the offset); empty or invalid values render as an em dash so table
 * cells stay stable. Never throws.
 */
export function formatDateTime(value: string | number | Date | null | undefined): string {
  if (value === null || value === undefined || value === "") {
    return "—";
  }
  const date = value instanceof Date ? value : new Date(value);
  if (Number.isNaN(date.getTime())) {
    return "—";
  }
  return format(date, DATE_TIME_PATTERN);
}
