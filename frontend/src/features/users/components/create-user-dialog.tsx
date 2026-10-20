import { useEffect, useState } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { Controller, useForm } from "react-hook-form";
import { Loader2 } from "lucide-react";
import { z } from "zod";

import { CategoryMultiSelect } from "@/components/shared/category-multi-select";
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
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { PasswordChecklist } from "@/features/auth/password-checklist";
import { passwordSchema } from "@/features/auth/schemas";
import { useCategories } from "@/features/categories/hooks";
import { isApiError } from "@/lib/error-mapping";

import { useCreateUser } from "../hooks";

/** Mirrors the backend CreateAdminUserRequest validators (integration §8). */
const createUserSchema = z
  .object({
    fullName: z
      .string()
      .min(2, "Full name must be at least 2 characters.")
      .max(100, "Full name must be 100 characters or fewer."),
    email: z.email("Enter a valid email address."),
    password: passwordSchema,
    role: z.enum(["Employee", "Agent"]),
    categoryIds: z.array(z.number().int()),
  })
  .superRefine((data, ctx) => {
    if (data.role === "Agent" && data.categoryIds.length === 0) {
      ctx.addIssue({
        code: "custom",
        path: ["categoryIds"],
        message: "Select at least one category for the agent.",
      });
    }
  });

type CreateUserInput = z.infer<typeof createUserSchema>;

/** P7 — create an Employee or Agent account (Agent requires categories). */
export function CreateUserDialog({
  open,
  onOpenChange,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}) {
  const categories = useCategories(true);
  const createUser = useCreateUser();
  const [serverError, setServerError] = useState<string | null>(null);

  const {
    register,
    control,
    handleSubmit,
    watch,
    setValue,
    setError,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<CreateUserInput>({
    resolver: zodResolver(createUserSchema),
    defaultValues: {
      fullName: "",
      email: "",
      password: "",
      role: "Employee",
      categoryIds: [],
    },
  });

  const role = watch("role");
  const password = watch("password");

  useEffect(() => {
    if (open) {
      reset();
      setServerError(null);
    }
  }, [open, reset]);

  const close = (next: boolean) => {
    if (!isSubmitting) {
      onOpenChange(next);
    }
  };

  const onSubmit = handleSubmit(async (values) => {
    setServerError(null);
    try {
      await createUser.mutateAsync({
        fullName: values.fullName.trim(),
        email: values.email.trim(),
        password: values.password,
        role: values.role,
        categoryIds: values.role === "Agent" ? values.categoryIds : undefined,
      });
      close(false);
    } catch (error) {
      if (isApiError(error)) {
        setServerError(error.detail);
        if (error.fieldErrors) {
          for (const [field, messages] of Object.entries(error.fieldErrors)) {
            if (field === "fullName" || field === "email" || field === "password") {
              setError(field, { message: messages.join(" ") });
            }
          }
        }
      }
    }
  });

  return (
    <Dialog open={open} onOpenChange={close}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Create user</DialogTitle>
          <DialogDescription>Add an Employee or Agent account to the help desk.</DialogDescription>
        </DialogHeader>

        <form onSubmit={onSubmit} noValidate className="space-y-4">
          {serverError ? (
            <Alert variant="destructive">
              <AlertDescription>{serverError}</AlertDescription>
            </Alert>
          ) : null}

          <div>
            <Label htmlFor="new-user-fullname">Full name</Label>
            <Input
              id="new-user-fullname"
              className="mt-1.5"
              maxLength={100}
              autoComplete="off"
              aria-invalid={errors.fullName ? true : undefined}
              {...register("fullName")}
            />
            <FieldError message={errors.fullName?.message} />
          </div>

          <div>
            <Label htmlFor="new-user-email">Email</Label>
            <Input
              id="new-user-email"
              className="mt-1.5"
              type="email"
              autoComplete="off"
              aria-invalid={errors.email ? true : undefined}
              {...register("email")}
            />
            <FieldError message={errors.email?.message} />
          </div>

          <div>
            <Label htmlFor="new-user-password">Password</Label>
            <Input
              id="new-user-password"
              className="mt-1.5"
              type="password"
              autoComplete="new-password"
              aria-invalid={errors.password ? true : undefined}
              {...register("password")}
            />
            <FieldError message={errors.password?.message} />
            <PasswordChecklist value={password} />
          </div>

          <div>
            <Label htmlFor="new-user-role">Role</Label>
            <Select
              value={role}
              onValueChange={(value) =>
                setValue("role", value as CreateUserInput["role"], { shouldValidate: true })
              }
            >
              <SelectTrigger id="new-user-role" className="mt-1.5">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="Employee">Employee</SelectItem>
                <SelectItem value="Agent">Agent</SelectItem>
              </SelectContent>
            </Select>
          </div>

          {role === "Agent" ? (
            <div>
              <Label id="new-user-categories-label">Work categories</Label>
              <Controller
                control={control}
                name="categoryIds"
                render={({ field }) => (
                  <CategoryMultiSelect
                    id="new-user-categories"
                    aria-labelledby="new-user-categories-label"
                    className="mt-1.5"
                    value={field.value}
                    onValueChange={field.onChange}
                    options={(categories.data ?? []).map((c) => ({ id: c.id, name: c.name }))}
                    placeholder="Select the categories this agent will work"
                  />
                )}
              />
              <FieldError message={errors.categoryIds?.message} />
            </div>
          ) : null}

          <DialogFooter>
            <Button type="button" variant="ghost" disabled={isSubmitting} onClick={() => close(false)}>
              Cancel
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? <Loader2 className="animate-spin" aria-hidden="true" /> : null}
              Create user
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}