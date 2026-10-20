import * as React from "react";
import { ChevronLeft, ChevronRight } from "lucide-react";
import {
  flexRender,
  getCoreRowModel,
  useReactTable,
  type TableOptions,
} from "@tanstack/react-table";

import { Button } from "@/components/ui/button";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { cn } from "@/lib/utils";

const SKELETON_ROWS = 5;

type DataTableProps<TData> = {
  /**
   * Reuses TanStack's own `CoreOptions.columns` type (`ColumnDef<TData, any>[]`)
   * via indexed access so we don't write a literal `any` (lint-clean). Erasing
   * TValue is required so column defs with mixed accessor value types
   * (number | string | undefined …) unify in one array — TValue occurs in
   * contravariant positions (groupingFn, cell) and no concrete type unifies.
   */
  columns: TableOptions<TData>["columns"];
  data: TData[];
  /** Parent query is fetching — renders skeleton rows instead of data. */
  loading?: boolean;
  /** 1-based current page (server-side). */
  page: number;
  pageSize: number;
  totalCount: number;
  onPageChange: (page: number) => void;
  onPageSizeChange?: (pageSize: number) => void;
  pageSizeOptions?: number[];
  /** Rendered instead of the table when there are no rows (and not loading). */
  empty?: React.ReactNode;
  /** Makes each row a keyboard-accessible button (row-click navigation). */
  onRowClick?: (row: TData) => void;
  className?: string;
};

/**
 * shadcn-style data table on @tanstack/react-table with server-side
 * pagination bound to PagedResult (plan §8).
 */
export function DataTable<TData>({
  columns,
  data,
  loading,
  page,
  pageSize,
  totalCount,
  onPageChange,
  onPageSizeChange,
  pageSizeOptions = [10, 20, 50, 100],
  empty,
  onRowClick,
  className,
}: DataTableProps<TData>) {
  const table = useReactTable({
    data,
    columns,
    getCoreRowModel: getCoreRowModel(),
  });
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  const pageSizeId = React.useId();

  const activateRow = (row: TData) => {
    onRowClick?.(row);
  };

  return (
    <div className={cn("rounded-xl border bg-card", className)}>
      <Table>
        <TableHeader>
          {table.getHeaderGroups().map((headerGroup) => (
            <TableRow key={headerGroup.id}>
              {headerGroup.headers.map((header) => (
                <TableHead key={header.id} colSpan={header.colSpan}>
                  {header.isPlaceholder
                    ? null
                    : flexRender(header.column.columnDef.header, header.getContext())}
                </TableHead>
              ))}
            </TableRow>
          ))}
        </TableHeader>
        <TableBody>
          {loading ? (
            Array.from({ length: SKELETON_ROWS }).map((_, rowIndex) => (
              <TableRow key={rowIndex}>
                {Array.from({ length: columns.length }).map((_, cellIndex) => (
                  <TableCell key={cellIndex}>
                    <Skeleton className="h-4 w-full max-w-40" />
                  </TableCell>
                ))}
              </TableRow>
            ))
          ) : data.length === 0 ? (
            <TableRow className="hover:bg-transparent">
              <TableCell colSpan={columns.length} className="p-0">
                {empty ?? (
                  <p className="px-4 py-12 text-center text-sm text-muted-foreground">
                    No items found.
                  </p>
                )}
              </TableCell>
            </TableRow>
          ) : (
            table.getRowModel().rows.map((row) => (
              <TableRow
                key={row.id}
                className={onRowClick ? "cursor-pointer" : undefined}
                onClick={onRowClick ? () => activateRow(row.original) : undefined}
                onKeyDown={
                  onRowClick
                    ? (event) => {
                        if (event.key === "Enter" || event.key === " ") {
                          event.preventDefault();
                          activateRow(row.original);
                        }
                      }
                    : undefined
                }
                role={onRowClick ? "button" : undefined}
                tabIndex={onRowClick ? 0 : undefined}
              >
                {row.getVisibleCells().map((cell) => (
                  <TableCell key={cell.id}>
                    {flexRender(cell.column.columnDef.cell, cell.getContext())}
                  </TableCell>
                ))}
              </TableRow>
            ))
          )}
        </TableBody>
      </Table>
      <div className="flex flex-col-reverse gap-2 border-t px-3 py-2.5 sm:flex-row sm:items-center sm:justify-between">
        <p className="text-xs text-muted-foreground">
          {totalCount === 1 ? "1 item" : `${totalCount} items`}
        </p>
        <div className="flex flex-wrap items-center gap-2">
          {onPageSizeChange ? (
            <div className="flex items-center gap-1.5">
              <label htmlFor={pageSizeId} className="text-xs text-muted-foreground">
                Rows per page
              </label>
              <Select value={String(pageSize)} onValueChange={(value) => onPageSizeChange(Number(value))}>
                <SelectTrigger id={pageSizeId} size="sm" className="h-7 w-16">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {pageSizeOptions.map((option) => (
                    <SelectItem key={option} value={String(option)}>
                      {option}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          ) : null}
          <Button
            variant="outline"
            size="icon-sm"
            aria-label="Previous page"
            disabled={page <= 1}
            onClick={() => onPageChange(page - 1)}
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
            onClick={() => onPageChange(page + 1)}
          >
            <ChevronRight aria-hidden="true" />
          </Button>
        </div>
      </div>
    </div>
  );
}
