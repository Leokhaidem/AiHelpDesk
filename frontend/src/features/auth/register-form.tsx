import { toast } from "sonner";
import { zodResolver } from "@hookform/resolvers/zod";
import { Controller, useForm } from "react-hook-form";
import { useState } from "react";
import { useNavigate } from "react-router";

import { CategoryMultiSelect } from "@/components/shared/category-multi-select";
import { FieldError } from "@/components/shared/field-error";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Switch } from "@/components/ui/switch";
import { Textarea } from "@/components/ui/textarea";
import { usePublicCategories } from "@/features/categories/hooks";
import { isApiError } from "@/lib/error-mapping";
import { useAuthStore } from "@/stores/auth-store";

import { authApi } from "./api";
import { PasswordChecklist } from "./password-checklist";
import { registerSchema, type RegisterInput } from "./schemas";

export function RegisterForm() {
  const navigate = useNavigate();
  const setSession = useAuthStore((state) => state.setSession);
  const { data: publicCategories } = usePublicCategories();
  const [serverError, setServerError] = useState<string | null>(null);

  const {
    register,
    control,
    handleSubmit,
    watch,
    setValue,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<RegisterInput>({
    resolver: zodResolver(registerSchema),
    defaultValues: {
      fullName: "",
      email: "",
      password: "",
      applyAsAgent: false,
      categoryIds: [],
      agentMessage: "",
    },
  });

  const applyAsAgent = watch("applyAsAgent");
  const passwordValue = watch("password");

  const onSubmit = handleSubmit(async (values) => {
    setServerError(null);
    try {
      const result = await authApi.register({
        fullName: values.fullName,
        email: values.email,
        password: values.password,
        applyAsAgent: values.applyAsAgent,
        categoryIds: values.applyAsAgent ? values.categoryIds : [],
        agentMessage: values.agentMessage?.trim() ? values.agentMessage.trim() : undefined,
      });
      setSession({
        accessToken: result.accessToken,
        expiresAt: new Date(result.expiresAt).getTime(),
      });
      if (result.agentApplication) {
        toast.success("Your agent application has been submitted for review.");
      }
      navigate("/", { replace: true });
    } catch (error) {
      if (isApiError(error)) {
        setServerError(error.detail);
        if (error.fieldErrors) {
          for (const [field, messages] of Object.entries(error.fieldErrors)) {
            if (field in values) {
              setError(field as keyof RegisterInput, { message: messages.join(" ") });
            }
          }
        }
      }
    }
  });

  return (
    <div className="rounded-lg border border-neutral-200 bg-white p-6 shadow-sm">
      <form onSubmit={onSubmit} noValidate>
        {serverError ? (
          <Alert variant="destructive" className="mb-4">
            <AlertDescription>{serverError}</AlertDescription>
          </Alert>
        ) : null}

        <div className="space-y-4">
          <div>
            <Label htmlFor="register-fullname">Full name</Label>
            <Input
              id="register-fullname"
              autoComplete="name"
              className="mt-1.5"
              aria-invalid={errors.fullName ? true : undefined}
              {...register("fullName")}
            />
            <FieldError message={errors.fullName?.message} />
          </div>

          <div>
            <Label htmlFor="register-email">Email</Label>
            <Input
              id="register-email"
              type="email"
              autoComplete="email"
              className="mt-1.5"
              aria-invalid={errors.email ? true : undefined}
              {...register("email")}
            />
            <FieldError message={errors.email?.message} />
          </div>

          <div>
            <Label htmlFor="register-password">Password</Label>
            <Input
              id="register-password"
              type="password"
              autoComplete="new-password"
              className="mt-1.5"
              aria-invalid={errors.password ? true : undefined}
              {...register("password")}
            />
            <FieldError message={errors.password?.message} />
            <PasswordChecklist value={passwordValue} />
          </div>
          <div className="flex items-center justify-between gap-3 rounded-md border border-neutral-200 px-3 py-2.5">
            <div>
              <Label htmlFor="register-apply-as-agent" className="text-sm font-medium">
                Apply as an agent
              </Label>
              <p className="mt-0.5 text-xs text-neutral-500">
                Requires approval from an admin before you can work tickets.
              </p>
            </div>
            <Switch
              id="register-apply-as-agent"
              checked={applyAsAgent}
              onCheckedChange={(checked) => setValue("applyAsAgent", checked, { shouldValidate: true })}
            />
          </div>

          {applyAsAgent ? (
            <div className="space-y-4 rounded-md border border-neutral-200 p-3">
              <div>
                <Label id="register-categories-label">Work categories</Label>
                <Controller
                  control={control}
                  name="categoryIds"
                  render={({ field }) => (
                    <CategoryMultiSelect
                      id="register-categories"
                      aria-labelledby="register-categories-label"
                      className="mt-1.5"
                      value={field.value}
                      onValueChange={field.onChange}
                      options={(publicCategories ?? []).map((c) => ({ id: c.id, name: c.name }))}
                      placeholder="Select the categories you can work"
                    />
                  )}
                />
                <FieldError message={errors.categoryIds?.message} />
              </div>

              <div>
                <Label htmlFor="register-agent-message">Message (optional)</Label>
                <Textarea
                  id="register-agent-message"
                  rows={3}
                  className="mt-1.5"
                  placeholder="Briefly describe your experience with this kind of support work"
                  {...register("agentMessage")}
                />
                <FieldError message={errors.agentMessage?.message} />
              </div>
            </div>
          ) : null}

          <Button type="submit" className="w-full" disabled={isSubmitting}>
            {isSubmitting ? "Creating account..." : "Create account"}
          </Button>
        </div>
      </form>
    </div>
  );
}
