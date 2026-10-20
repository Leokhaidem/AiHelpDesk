import type { ReactNode } from "react";
import { LifeBuoy } from "lucide-react";

type AuthShellProps = {
  title: string;
  subtitle?: string;
  children: ReactNode;
  footer?: ReactNode;
};

/** Centered single-column layout shared by the public auth pages. */
export function AuthShell({ title, subtitle, children, footer }: AuthShellProps) {
  return (
    <main className="flex min-h-screen items-center justify-center bg-neutral-50 p-4">
      <div className="w-full max-w-sm">
        <div className="mb-6 flex flex-col items-center gap-3 text-center">
          <div className="flex items-center gap-2">
            <LifeBuoy className="h-6 w-6 text-neutral-900" aria-hidden="true" />
            <span className="text-lg font-semibold tracking-tight text-neutral-900">
              AI Help Desk
            </span>
          </div>
          <div>
            <h1 className="text-xl font-semibold tracking-tight text-neutral-900">{title}</h1>
            {subtitle ? <p className="mt-1 text-sm text-neutral-600">{subtitle}</p> : null}
          </div>
        </div>
        {children}
        {footer ? <div className="mt-4 text-center text-sm text-neutral-600">{footer}</div> : null}
      </div>
    </main>
  );
}
