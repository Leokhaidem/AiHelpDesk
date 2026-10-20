/**
 * Normalized API error. The backend returns four distinct error body shapes
 * (see docs/frontend-integration.md, section 6); everything the UI needs goes
 * through this single type.
 */
export type ApiError = {
  status: number;
  /** User-safe summary, suitable for a toast or heading. */
  title: string;
  /** User-safe detail, safe to display verbatim. */
  detail: string;
  /** Field-level validation messages. Keys are lowercased C# property names. */
  fieldErrors?: Record<string, string[]>;
  /** Original payload, kept for debugging only. Never rendered. */
  raw: unknown;
};

export function isApiError(value: unknown): value is ApiError {
  if (typeof value !== "object" || value === null) {
    return false;
  }
  const candidate = value as Partial<ApiError>;
  return typeof candidate.status === "number" && typeof candidate.detail === "string";
}

function emptyBodyError(status: number, raw: unknown): ApiError {
  if (status === 404) {
    return { status, title: "Not found", detail: "The requested resource was not found.", raw };
  }
  return {
    status,
    title: "Request failed",
    detail: "The request could not be completed. Please try again.",
    raw,
  };
}

function isPlainObject(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

/**
 * Maps one of the four backend error shapes (integration doc section 6) onto
 * a single ApiError.
 */
function mapBody(status: number, data: unknown): ApiError {
  // Shape 2: raw JSON string, e.g. "Invalid email or password."
  if (typeof data === "string") {
    const message = data.trim();
    return {
      status,
      title: "Request failed",
      detail: message.length > 0 ? message : "The request could not be completed.",
      raw: data,
    };
  }

  if (isPlainObject(data)) {
    // Shape 4: FluentValidation — { title, status, errors: { Title: [...] } }
    if (isPlainObject(data.errors)) {
      const fieldErrors: Record<string, string[]> = {};
      for (const [key, value] of Object.entries(data.errors)) {
        const messages = Array.isArray(value)
          ? value.filter((v): v is string => typeof v === "string")
          : typeof value === "string"
            ? [value]
            : [];
        if (messages.length > 0) {
          fieldErrors[key.toLowerCase()] = messages;
        }
      }
      return {
        status,
        title:
          typeof data.title === "string" && data.title.length > 0
            ? data.title
            : "Validation failed",
        detail: "Please correct the validation errors.",
        fieldErrors,
        raw: data,
      };
    }

    // Shape 3: { message: "..." } or { errors: ["...", "..."] }
    if (typeof data.message === "string") {
      return { status, title: "Request failed", detail: data.message, raw: data };
    }
    if (Array.isArray(data.errors)) {
      const messages = data.errors.filter((v): v is string => typeof v === "string");
      return {
        status,
        title: "Request failed",
        detail: messages.length > 0 ? messages.join(" ") : "The request could not be completed.",
        raw: data,
      };
    }

    // Shape 1: RFC 7807 ProblemDetails — { status, title, detail, instance }
    if (typeof data.detail === "string" || typeof data.title === "string") {
      return {
        status,
        title:
          typeof data.title === "string" && data.title.length > 0 ? data.title : "Request failed",
        detail:
          typeof data.detail === "string" && data.detail.length > 0
            ? data.detail
            : "The request could not be completed.",
        raw: data,
      };
    }
  }

  return emptyBodyError(status, data);
}

/**
 * Converts any rejected value from the axios client into an ApiError.
 * Non-HTTP failures (network, timeout) produce status 0.
 */
export function toApiError(error: unknown): ApiError {
  if (error !== null && typeof error === "object" && "response" in error) {
    const axiosError = error as { response?: { status?: number; data?: unknown } };
    const status = typeof axiosError.response?.status === "number" ? axiosError.response.status : 0;
    return mapBody(status, axiosError.response?.data);
  }

  if (error instanceof Error) {
    const isTimeout = /timeout/i.test(error.message);
    return {
      status: 0,
      title: "Connection problem",
      detail: isTimeout
        ? "The request timed out. Please try again."
        : "Unable to reach the server. Check your connection and try again.",
      raw: error,
    };
  }

  return {
    status: 0,
    title: "Unexpected error",
    detail: "Something went wrong. Please try again.",
    raw: error,
  };
}
