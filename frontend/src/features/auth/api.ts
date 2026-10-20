import { apiClient } from "@/lib/api-client";

import type { AgentApplicationResponse } from "@/features/applications/api";

export type LoginRequest = {
  email: string;
  password: string;
};

export type AuthResult = {
  accessToken: string;
  /** ISO-8601 UTC. */
  expiresAt: string;
};

export type RegisterRequest = {
  fullName: string;
  email: string;
  password: string;
  applyAsAgent: boolean;
  categoryIds: number[];
  agentMessage?: string;
};

export type RegisterResult = AuthResult & {
  /** Set only when applyAsAgent is true; otherwise null. */
  agentApplication: AgentApplicationResponse | null;
};

export const authApi = {
  login: (body: LoginRequest) =>
    apiClient.post<AuthResult>("/api/auth/login", body).then((r) => r.data),
  register: (body: RegisterRequest) =>
    apiClient.post<RegisterResult>("/api/auth/register", body).then((r) => r.data),
};
