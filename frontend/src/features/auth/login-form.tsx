import { useState } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { useNavigate, useSearchParams } from "react-router";

import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { isApiError } from "@/lib/error-mapping";
import { useAuthStore } from "@/stores/auth-store";

import { authApi } from "./api";
import { loginSchema, type LoginInput } from "./schemas";

export function LoginForm() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const setSession = useAuthStore((state) => state.setSession);
  const [serverError, setServerError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginInput>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: "", password: "" },
  });

  const onSubmit = handleSubmit(async (values) => {
    setServerError(null);
    try {
      const result = await authApi.login(values);
      setSession({
        accessToken: result.accessToken,
        expiresAt: new Date(result.expiresAt).getTime(),
      });
      const next = searchParams.get("next");
      navigate(next && next.startsWith("/") ? next : "/", { replace: true });
    } catch (error) {
      if (isApiError(error)) {
        setServerError(error.detail);
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
            <Label htmlFor="login-email">Email</Label>
            <Input
              id="login-email"
              type="email"
              autoComplete="email"
              placeholder="you@company.com"
              className="mt-1.5"
              aria-invalid={errors.email ? true : undefined}
              {...register("email")}
            />
            {errors.email ? (
              <p className="mt-1 text-sm text-red-600" role="alert">
                {errors.email.message}
              </p>
            ) : null}
          </div>

          <div>
            <Label htmlFor="login-password">Password</Label>
            <Input
              id="login-password"
              type="password"
              autoComplete="current-password"
              className="mt-1.5"
              aria-invalid={errors.password ? true : undefined}
              {...register("password")}
            />
            {errors.password ? (
              <p className="mt-1 text-sm text-red-600" role="alert">
                {errors.password.message}
              </p>
            ) : null}
          </div>

          <Button type="submit" className="w-full" disabled={isSubmitting}>
            {isSubmitting ? "Signing in..." : "Sign in"}
          </Button>
        </div>
      </form>
    </div>
  );
}
