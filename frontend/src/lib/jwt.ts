/**
 * JWT parsing. The token is parsed once at login (never re-parsed on request);
 * the claims layout is documented in docs/frontend-integration.md, section 4.3.
 *
 * Claim keys use long-form URIs:
 *  - nameidentifier (user id)
 *  - role (one claim per role; .NET collapses duplicates into an array)
 */
export type JwtUser = {
  id: string;
  email: string;
  roles: string[];
};

const EMAIL_CLAIM = "email";
const NAME_IDENTIFIER_CLAIM =
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";
const ROLE_CLAIM = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

function base64UrlDecode(input: string): string {
  const base64 = input.replace(/-/g, "+").replace(/_/g, "/");
  const padded = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), "=");
  const binary = atob(padded);
  const bytes = Uint8Array.from(binary, (char) => char.charCodeAt(0));
  return new TextDecoder().decode(bytes);
}

function asStringArray(value: unknown): string[] {
  if (Array.isArray(value)) {
    return value.filter((v): v is string => typeof v === "string");
  }
  if (typeof value === "string" && value.length > 0) {
    return [value];
  }
  return [];
}

export function parseJwt(token: string): JwtUser {
  const payloadSegment = token.split(".")[1];
  if (!payloadSegment) {
    throw new Error("Invalid token: missing payload segment");
  }

  let payload: Record<string, unknown>;
  try {
    payload = JSON.parse(base64UrlDecode(payloadSegment)) as Record<string, unknown>;
  } catch {
    throw new Error("Invalid token: payload is not valid JSON");
  }

  const id =
    typeof payload[NAME_IDENTIFIER_CLAIM] === "string"
      ? payload[NAME_IDENTIFIER_CLAIM]
      : typeof payload.sub === "string"
        ? payload.sub
        : "";
  const email = typeof payload[EMAIL_CLAIM] === "string" ? payload[EMAIL_CLAIM] : "";
  const roles = asStringArray(payload[ROLE_CLAIM]);

  if (id.length === 0 || email.length === 0) {
    throw new Error("Invalid token: missing required claims");
  }

  return { id, email, roles };
}
