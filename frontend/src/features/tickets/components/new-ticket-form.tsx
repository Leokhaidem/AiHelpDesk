import { useState } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { useNavigate } from "react-router";
import { Loader2, Send } from "lucide-react";
import { z } from "zod";

import { FieldError } from "@/components/shared/field-error";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { isApiError } from "@/lib/error-mapping";

import { useCreateTicket } from "../hooks";

/** Mirrors the backend CreateTicketRequestValidator (integration §8). */
const newTicketSchema = z.object({
  title: z
    .string()
    .min(1, "Title is required.")
    .max(200, "Title must not exceed 200 characters."),
  description: z
    .string()
    .min(20, "Description must be at least 20 characters so the AI can analyze it properly.")
    .max(5000, "Description must not exceed 5000 characters."),
});

type NewTicketInput = z.infer<typeof newTicketSchema>;

/**
 * P18 — new ticket form. On success the caller navigates to the ticket
 * detail, where the AI banner shows "analyzing…" (5 s polling).
 */
export function NewTicketForm() {
  const navigate = useNavigate();
  const createTicket = useCreateTicket();
  const [serverError, setServerError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    watch,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<NewTicketInput>({
    resolver: zodResolver(newTicketSchema),
    defaultValues: { title: "", description: "" },
  });

  const title = watch("title");
  const description = watch("description");

  const onSubmit = handleSubmit(async (values) => {
    setServerError(null);
    try {
      const ticket = await createTicket.mutateAsync({
        title: values.title.trim(),
        description: values.description.trim(),
      });
      navigate(`/employee/tickets/${ticket.id}`);
    } catch (error) {
      if (isApiError(error)) {
        setServerError(error.detail);
        if (error.fieldErrors) {
          for (const [field, messages] of Object.entries(error.fieldErrors)) {
            if (field === "title" || field === "description") {
              setError(field, { message: messages.join(" ") });
            }
          }
        }
      }
    }
  });

  return (
    <form onSubmit={onSubmit} noValidate className="space-y-4">
      {serverError ? (
        <Alert variant="destructive">
          <AlertDescription>{serverError}</AlertDescription>
        </Alert>
      ) : null}

      <div>
        <Label htmlFor="new-ticket-title">Title</Label>
        <Input
          id="new-ticket-title"
          className="mt-1.5"
          maxLength={200}
          placeholder="Short summary of your issue"
          aria-invalid={errors.title ? true : undefined}
          {...register("title")}
        />
        <div className="mt-1 flex items-center justify-between gap-2">
          <FieldError message={errors.title?.message} />
          <span className="ml-auto text-xs text-muted-foreground tabular-nums">
            {title.length}/200
          </span>
        </div>
      </div>

      <div>
        <Label htmlFor="new-ticket-description">Description</Label>
        <Textarea
          id="new-ticket-description"
          rows={7}
          className="mt-1.5"
          maxLength={5000}
          placeholder="Describe the problem in detail — what happened, what you expected, and any steps to reproduce it."
          aria-invalid={errors.description ? true : undefined}
          {...register("description")}
        />
        <div className="mt-1 flex items-center justify-between gap-2">
          <FieldError message={errors.description?.message} />
          <span className="ml-auto text-xs text-muted-foreground tabular-nums">
            {description.length}/5000
          </span>
        </div>
        <p className="mt-1 text-xs text-muted-foreground">
          The AI uses this text to classify your ticket and suggest a resolution. At least 20
          characters.
        </p>
      </div>

      <Button type="submit" disabled={isSubmitting}>
        {isSubmitting ? (
          <Loader2 className="animate-spin" aria-hidden="true" />
        ) : (
          <Send aria-hidden="true" />
        )}
        Submit ticket
      </Button>
    </form>
  );
}