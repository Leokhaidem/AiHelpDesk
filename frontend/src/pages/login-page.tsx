import { Navigate } from "react-router";
import { Link } from "react-router";

import { AuthShell } from "@/features/auth/auth-shell";
import { LoginForm } from "@/features/auth/login-form";
import { useAuthStore } from "@/stores/auth-store";

export function LoginPage() {
  const status = useAuthStore((state) => state.status);

  if (status === "authenticated") {
    return <Navigate to="/" replace />;
  }

  return (
    <AuthShell
      title="Sign in"
      subtitle="Use your work account to continue."
      footer={
        <>
          New here?{" "}
          <Link
            to="/register"
            className="font-medium text-neutral-900 underline underline-offset-2"
          >
            Create an account
          </Link>
        </>
      }
    >
      <LoginForm />
    </AuthShell>
  );
}
