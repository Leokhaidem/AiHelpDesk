import { toast } from "sonner";

import { isApiError } from "@/lib/error-mapping";

/**
 * Toasts a user-safe message for any rejected API value (plan §11).
 * Non-API rejections (bugs, network edge cases) get a generic message.
 */
export function toastApiError(error: unknown, fallback: string) {
  toast.error(isApiError(error) ? error.detail : fallback);
}
