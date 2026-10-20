import { useEffect, useState } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { Loader2 } from "lucide-react";
import { z } from "zod";

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
import { isApiError } from "@/lib/error-mapping";

import { useUpdateUserProfile } from "../hooks";

import type { AdminUser } from "../api";

const editUserSchema = z.object({
  fullName: z
    .string()
    .min(2, "Full name must be at least 2 characters.")
    .max(100, "Full name must be 100 characters or fewer."),
  email: z.email("Enter a valid email address."),
});

type EditUserInput = z.infer<typeof editUserSchema>;

/** P8 — edit the user's profile (full name + email). */
export function UserEditDialog({
  user,
  open,
  onOpenChange,
}: {
  user: AdminUser;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}) {
  const updateProfile = useUpdateUserProfile();
  const [serverError, setServerError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    setError,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<EditUserInput>({
    resolver: zodResolver(editUserSchema),
    defaultValues: { fullName: user.fullName, email: user.email },
  });

  useEffect(() => {
    if (open) {
      reset({ fullName: user.fullName, email: user.email });
      setServerError(null);
    }
  }, [open, reset, user]);

  const close = (next: boolean) => {
    if (!isSubmitting) {
      onOpenChange(next);
    }
  };

  const onSubmit = handleSubmit(async (values) => {
    setServerError(null);
    try {
      await updateProfile.mutateAsync({
        id: user.id,
        fullName: values.fullName.trim(),
        email: values.email.trim(),
      });
      close(false);
    } catch (error) {
      if (isApiError(error)) {
        setServerError(error.detail);
        if (error.fieldErrors) {
          for (const [field, messages] of Object.entries(error.fieldErrors)) {
            if (field === "fullName" || field === "email") {
              setError(field, { message: messages.join(" ") });
            }
          }
        }
      }
    }
  });

  return (
    <Dialog open={open} onOpenChange={close}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Edit profile</DialogTitle>
          <DialogDescription>Update the user's name or email.</DialogDescription>
        </DialogHeader>

        <form onSubmit={onSubmit} noValidate className="space-y-4">
          {serverError ? (
            <Alert variant="destructive">
              <AlertDescription>{serverError}</AlertDescription>
            </Alert>
          ) : null}
          <div>
            <Label htmlFor="edit-user-fullname">Full name</Label>
            <Input
              id="edit-user-fullname"
              className="mt-1.5"
              maxLength={100}
              autoComplete="off"
              aria-invalid={errors.fullName ? true : undefined}
              {...register("fullName")}
            />
            <FieldError message={errors.fullName?.message} />
          </div>
          <div>
            <Label htmlFor="edit-user-email">Email</Label>
            <Input
              id="edit-user-email"
              className="mt-1.5"
              type="email"
              autoComplete="off"
              aria-invalid={errors.email ? true : undefined}
              {...register("email")}
            />
            <FieldError message={errors.email?.message} />
          </div>
          <DialogFooter>
            <Button type="button" variant="ghost" disabled={isSubmitting} onClick={() => close(false)}>
              Cancel
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? <Loader2 className="animate-spin" aria-hidden="true" /> : null}
              Save changes
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}