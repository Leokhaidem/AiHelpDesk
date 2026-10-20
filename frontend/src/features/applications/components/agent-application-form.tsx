import { useState, type FormEvent } from "react";
import { Loader2, Send } from "lucide-react";

import { CategoryMultiSelect } from "@/components/shared/category-multi-select";
import { FieldError } from "@/components/shared/field-error";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { useCategories } from "@/features/categories/hooks";
import { isApiError } from "@/lib/error-mapping";

import { useCreateApplication } from "../hooks";

const MESSAGE_MAX = 1000;

/**
 * P21 — apply for the Agent role (also used to re-apply after a rejected
 * or withdrawn application). On success the query invalidation flips the
 * page to the "Pending" status card.
 */
export function AgentApplicationForm() {
  const categories = useCategories(true);
  const createApplication = useCreateApplication();
  const [categoryIds, setCategoryIds] = useState<number[]>([]);
  const [message, setMessage] = useState("");
  const [touched, setTouched] = useState(false);

  const categoryError =
    categoryIds.length === 0 ? "Select at least one category." : undefined;
  const messageError =
    message.length > MESSAGE_MAX
      ? `Message must be ${MESSAGE_MAX} characters or fewer.`
      : undefined;

  const onSubmit = (event: FormEvent) => {
    event.preventDefault();
    setTouched(true);
    if (categoryError || messageError || createApplication.isPending) {
      return;
    }
    createApplication.mutate({
      categoryIds,
      message: message.trim() ? message.trim() : undefined,
    });
  };

  return (
    <form onSubmit={onSubmit} noValidate className="space-y-4">
      {createApplication.isError && isApiError(createApplication.error) ? (
        <Alert variant="destructive">
          <AlertDescription>{createApplication.error.detail}</AlertDescription>
        </Alert>
      ) : null}
      {categories.isError ? (
        <Alert variant="destructive">
          <AlertDescription>
            Categories could not be loaded. Try again in a moment.
          </AlertDescription>
        </Alert>
      ) : null}

      <div>
        <Label id="application-categories-label">Work categories</Label>
        <CategoryMultiSelect
          id="application-categories"
          aria-labelledby="application-categories-label"
          className="mt-1.5"
          value={categoryIds}
          onValueChange={setCategoryIds}
          options={(categories.data ?? []).map((c) => ({ id: c.id, name: c.name }))}
          placeholder="Select the categories you can work"
          disabled={categories.isPending || createApplication.isPending}
        />
        <FieldError message={touched ? categoryError : undefined} />
        <p className="mt-1 text-xs text-muted-foreground">
          You will only be assigned tickets in these categories.
        </p>
      </div>

      <div>
        <Label htmlFor="application-message">Message (optional)</Label>
        <Textarea
          id="application-message"
          rows={4}
          className="mt-1.5"
          maxLength={MESSAGE_MAX}
          placeholder="Briefly describe your experience with this kind of support work"
          value={message}
          onChange={(event) => setMessage(event.target.value)}
          onBlur={() => setTouched(true)}
          aria-invalid={messageError ? true : undefined}
          disabled={createApplication.isPending}
        />
        <div className="mt-1 flex items-center justify-between gap-2">
          <FieldError message={touched ? messageError : undefined} />
          <span className="ml-auto text-xs text-muted-foreground tabular-nums">
            {message.length}/{MESSAGE_MAX}
          </span>
        </div>
      </div>

      <Button type="submit" disabled={createApplication.isPending || categories.isPending}>
        {createApplication.isPending ? (
          <Loader2 className="animate-spin" aria-hidden="true" />
        ) : (
          <Send aria-hidden="true" />
        )}
        Submit application
      </Button>
    </form>
  );
}