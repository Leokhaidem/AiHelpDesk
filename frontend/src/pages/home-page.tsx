import { useState } from "react";

import { Button } from "@/components/ui/button";

/**
 * Temporary scaffold check page. Replaced by the real route tree in M2.
 */
export function HomePage() {
  const [count, setCount] = useState(0);

  return (
    <main className="flex min-h-screen items-center justify-center bg-neutral-50 p-4">
      <div className="w-full max-w-sm rounded-lg border border-neutral-200 bg-white p-8 text-center shadow-sm">
        <h1 className="text-lg font-semibold text-neutral-900">AI Help Desk</h1>
        <p className="mt-1 text-sm text-neutral-600">Frontend scaffold is running.</p>
        <Button className="mt-4" onClick={() => setCount((c) => c + 1)}>
          {count === 0 ? "Stack check" : `Clicked ${count} times`}
        </Button>
      </div>
    </main>
  );
}
