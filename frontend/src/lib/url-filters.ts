import { useCallback } from "react";
import { useSearchParams } from "react-router";

/**
 * URL-synced filter helpers (plan §11): filters live in search params so
 * links are shareable and back/forward works. Changing a filter resets the
 * `page` param back to 1; updates use replace() to avoid history spam.
 */

const EMPTY_VALUES = new Set(["", "null", "undefined"]);

function cleanValue(value: string | null): string | undefined {
  if (value === null || EMPTY_VALUES.has(value)) {
    return undefined;
  }
  return value;
}

/** Reads and writes one search param; `undefined` removes it. */
export function useUrlParam(key: string): [string | undefined, (value: string | undefined) => void] {
  const [searchParams, setSearchParams] = useSearchParams();
  const value = cleanValue(searchParams.get(key));

  const setValue = useCallback(
    (nextValue: string | undefined) => {
      setSearchParams(
        (prev) => {
          const next = new URLSearchParams(prev);
          const current = cleanValue(next.get(key));
          if (nextValue === undefined) {
            next.delete(key);
          } else {
            next.set(key, nextValue);
          }
          if (key !== "page" && current !== nextValue) {
            next.delete("page");
          }
          return next;
        },
        { replace: true },
      );
    },
    [key, setSearchParams],
  );

  return [value, setValue];
}

/** 1-based `page` search param, default 1. Page 1 omits the param. */
export function usePageParam(): [number, (page: number) => void] {
  const [searchParams, setSearchParams] = useSearchParams();
  const raw = Number.parseInt(searchParams.get("page") ?? "1", 10);
  const page = Number.isFinite(raw) && raw > 0 ? raw : 1;

  const setPage = useCallback(
    (nextPage: number) => {
      setSearchParams(
        (prev) => {
          const next = new URLSearchParams(prev);
          if (nextPage <= 1) {
            next.delete("page");
          } else {
            next.set("page", String(nextPage));
          }
          return next;
        },
        { replace: true },
      );
    },
    [setSearchParams],
  );

  return [page, setPage];
}

/** Parses a positive-integer search param; undefined when absent or invalid. */
export function toPositiveInt(value: string | undefined): number | undefined {
  if (!value) {
    return undefined;
  }
  const parsed = Number.parseInt(value, 10);
  return Number.isFinite(parsed) && parsed > 0 ? parsed : undefined;
}

/** Removes a set of search params — the "clear filters" action. */
export function useClearParams(): (keys: string[]) => void {
  const [, setSearchParams] = useSearchParams();
  return useCallback(
    (keys: string[]) => {
      setSearchParams(
        (prev) => {
          const next = new URLSearchParams(prev);
          for (const key of keys) {
            next.delete(key);
          }
          return next;
        },
        { replace: true },
      );
    },
    [setSearchParams],
  );
}
