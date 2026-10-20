import { create } from "zustand";

import { parseJwt, type JwtUser } from "@/lib/jwt";

const STORAGE_KEY = "aihelpdesk.session";

export type AuthStatus = "unauthenticated" | "authenticated";

type PersistedSession = {
  token: string;
  /** Epoch milliseconds. */
  expiresAt: number;
  user: JwtUser;
};

export type AuthState = {
  token: string | null;
  expiresAt: number | null;
  user: JwtUser | null;
  status: AuthStatus;
  /**
   * True while a deliberate logout is in progress so the 401 interceptor does
   * not fight it (integration doc section 4.4: deactivation also produces 401).
   */
  isLoggingOut: boolean;
  /** Parses the JWT, persists the session and marks the app authenticated. */
  setSession: (payload: { accessToken: string; expiresAt: number }) => void;
  /** Local-only logout: clears storage and state (the backend is stateless). */
  logout: () => void;
  /** Runs once at boot: restores a session if the token is still unexpired. */
  initFromStorage: () => void;
};

function readStorage(): PersistedSession | null {
  try {
    const raw = window.localStorage.getItem(STORAGE_KEY);
    if (!raw) {
      return null;
    }
    const parsed = JSON.parse(raw) as PersistedSession;
    if (
      typeof parsed.token !== "string" ||
      typeof parsed.expiresAt !== "number" ||
      typeof parsed.user?.id !== "string"
    ) {
      return null;
    }
    return parsed;
  } catch {
    return null;
  }
}

function writeStorage(session: PersistedSession): void {
  try {
    window.localStorage.setItem(STORAGE_KEY, JSON.stringify(session));
  } catch {
    // Storage unavailable (private mode quota, etc.) — session lives in memory only.
  }
}

function clearStorage(): void {
  try {
    window.localStorage.removeItem(STORAGE_KEY);
  } catch {
    // Nothing to clear.
  }
}

export const useAuthStore = create<AuthState>()((set) => ({
  token: null,
  expiresAt: null,
  user: null,
  status: "unauthenticated",
  isLoggingOut: false,

  setSession: ({ accessToken, expiresAt }) => {
    let user: JwtUser;
    try {
      user = parseJwt(accessToken);
    } catch {
      user = { id: "", email: "", roles: [] };
    }
    writeStorage({ token: accessToken, expiresAt, user });
    set({ token: accessToken, expiresAt, user, status: "authenticated" });
  },

  logout: () => {
    set({ isLoggingOut: true });
    clearStorage();
    set({ token: null, expiresAt: null, user: null, status: "unauthenticated" });
    // Re-arm the flag after in-flight requests have settled.
    window.setTimeout(() => set({ isLoggingOut: false }), 0);
  },

  initFromStorage: () => {
    const session = readStorage();
    if (!session) {
      set({ status: "unauthenticated" });
      return;
    }
    if (session.expiresAt <= Date.now()) {
      // Expired: the server would 401 anyway, so drop the session immediately.
      clearStorage();
      set({ status: "unauthenticated" });
      return;
    }
    set({
      token: session.token,
      expiresAt: session.expiresAt,
      user: session.user,
      status: "authenticated",
    });
  },
}));
