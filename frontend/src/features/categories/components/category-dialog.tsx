import { useEffect, useState } from "react";
import { Loader2 } from "lucide-react";

import { FieldError } from "@/components/shared/field-error";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { isApiError } from "@/lib/error-mapping";

import { useCreateCategory, useUpdateCategory } from "../hooks";

import type { CategoryResponse } from "../api";

type CategoryDialogProps = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  /** When provided the dialog edits this category; otherwise it creates one. */
  category?: CategoryResponse;
};

/** P9 — create / edit a category (name + optional description). */
export function CategoryDialog({ open, onOpenChange, category }: CategoryDialogProps) {
  const createCategory = useCreateCategory();
  const updateCategory = useUpdateCategory();
  const isEdit = category !== undefined;

  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [touched, setTouched] = useState(false);
  const [serverError, setServerError] = useState<string | null>(null);

  // Re-seed the fields every time the dialog opens with a (possibly new)
  // target category.
  useEffect(() => {
    if (open) {
      setName(category?.name ?? "");
      setDescription(category?.description ?? "");
      setTouched(false);
      setServerError(null);
    }
  }, [open, category]);

  const nameError = name.trim().length === 0 ? "Name is required." : undefined;
  const pending = createCategory.isPending || updateCategory.isPending;

  const close = (next: boolean) => {
    if (pending) {
      return;
    }
    if (!next) {
      setName(category?.name ?? "");
      setDescription(category?.description ?? "");
      setTouched(false);
      setServerError(null);
    }
    onOpenChange(next);
  };

  const submit = () => {
    setTouched(true);
    setServerError(null);
    if (nameError || pending) {
      return;
    }
    const body = { name: name.trim(), description: description.trim() || undefined };
    const run = isEdit && category
      ? updateCategory.mutateAsync({ id: category.id, ...body })
      : createCategory.mutateAsync(body);
    void run.then(() => close(false)).catch((error) => {
      if (isApiError(error)) {
        setServerError(error.detail);
      }
    });
  };

  return (
    <Dialog open={open} onOpenChange={close}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{isEdit ? `Edit category ${category?.id}` : "New category"}</DialogTitle>
          <DialogDescription>
            {isEdit
              ? "Update the category name or description."
              : "Categories drive AI analysis and agent assignment."}
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          {serverError ? (
            <Alert variant="destructive">
              <AlertDescription>{serverError}</AlertDescription>
            </Alert>
          ) : null}
          <div>
            <Label htmlFor="category-name">Name</Label>
            <Input
              id="category-name"
              className="mt-1.5"
              maxLength={100}
              value={name}
              onChange={(event) => setName(event.target.value)}
              onBlur={() => setTouched(true)}
              aria-invalid={nameError ? true : undefined}
            />
            <FieldError message={touched ? nameError : undefined} />
          </div>
          <div>
            <Label htmlFor="category-description">Description (optional)</Label>
            <Textarea
              id="category-description"
              rows={3}
              className="mt-1.5"
              maxLength={500}
              value={description}
              onChange={(event) => setDescription(event.target.value)}
            />
          </div>
        </div>

        <DialogFooter>
          <Button variant="ghost" disabled={pending} onClick={() => close(false)}>
            Cancel
          </Button>
          <Button disabled={pending} onClick={submit}>
            {pending ? <Loader2 className="animate-spin" aria-hidden="true" /> : null}
            {isEdit ? "Save changes" : "Create category"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}