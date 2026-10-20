import { Check } from "lucide-react";

import { passwordRules } from "@/features/auth/schemas";

/**
 * Live password-policy checklist for the register form. The rules are the
 * same constants the Zod schema validates against (integration doc 8).
 */
export function PasswordChecklist({ value }: { value: string }) {
  return (
    <ul
      className="mt-2 grid grid-cols-1 gap-x-4 gap-y-1 sm:grid-cols-2"
      aria-label="Password requirements"
    >
      {passwordRules.map((rule) => {
        const satisfied = rule.test(value);
        return (
          <li
            key={rule.key}
            className={
              satisfied
                ? "flex items-center gap-1.5 text-xs text-neutral-700"
                : "flex items-center gap-1.5 text-xs text-neutral-500"
            }
          >
            <span
              className={
                satisfied
                  ? "flex h-3.5 w-3.5 items-center justify-center rounded-full bg-neutral-900"
                  : "h-3.5 w-3.5 rounded-full border border-neutral-300"
              }
              aria-hidden="true"
            >
              {satisfied ? <Check className="h-2.5 w-2.5 text-white" /> : null}
            </span>
            {rule.label}
          </li>
        );
      })}
    </ul>
  );
}
