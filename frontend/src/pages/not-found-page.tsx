import { SearchX } from "lucide-react";
import { Link } from "react-router";

import { Button } from "@/components/ui/button";

export function NotFoundPage() {
  return (
    <main className="flex min-h-screen items-center justify-center bg-neutral-50 p-4">
      <div className="w-full max-w-sm rounded-lg border border-neutral-200 bg-white p-8 text-center shadow-sm">
        <SearchX className="mx-auto h-8 w-8 text-neutral-400" aria-hidden="true" />
        <h1 className="mt-3 text-lg font-semibold text-neutral-900">Page not found</h1>
        <p className="mt-2 text-sm text-neutral-600">
          The page you are looking for does not exist or may have been moved.
        </p>
        <Button asChild className="mt-6">
          <Link to="/">Go to my home</Link>
        </Button>
      </div>
    </main>
  );
}
