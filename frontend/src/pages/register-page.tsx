import { Link, Navigate } from "react-router";

import { AuthShell } from "@/features/auth/auth-shell";
import { RegisterForm } from "@/features/auth/register-form";
import { useAuthStore } from "@/stores/auth-store";

export function RegisterPage() {
  const status = useAuthStore((state) => state.status);

  if (status === "authenticated") {
    return <Navigate to="/" replace />;
  }

  return (
    <AuthShell
      title="Create your account"
      subtitle="Employees sign in with the same account."
      footer={
        <>
          Already have an account?{" "}
          <Link to="/login" className="font-medium text-neutral-900 underline underline-offset-2">
            Sign in
          </Link>
        </>
      }
    >
      <RegisterForm />
    </AuthShell>
  );
}
