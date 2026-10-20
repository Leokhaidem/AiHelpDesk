import { ShieldOff } from "lucide-react";
import { Link } from "react-router";

import { Button } from "@/components/ui/button";

/**
 * Shown when the session is valid but the current user's roles do not cover
 * the requested area. The server would also refuse the API calls themselves —
 * this page is the UX layer for that (plan section 6).
 */
export function UnauthorizedPage() {
  return (
    <main className="flex min-h-screen items-center justify-center bg-neutral-50 p-4">
      <div className="w-full max-w-sm rounded-lg border border-neutral-200 bg-white p-8 text-center shadow-sm">
        <ShieldOff className="mx-auto h-8 w-8 text-neutral-400" aria-hidden="true" />
        <h1 className="mt-3 text-lg font-semibold text-neutral-900">Access denied</h1>
        <p className="mt-2 text-sm text-neutral-600">
          Your current role does not have access to this area.
        </p>
        <Button asChild className="mt-6">
          <Link to="/">Go to my home</Link>
        </Button>
      </div>
    </main>
  );
}
