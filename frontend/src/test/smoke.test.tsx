import type { ReactElement } from "react";
import { QueryClientProvider } from "@tanstack/react-query";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { MemoryRouter } from "react-router";

import { queryClient } from "@/lib/query-client";
import { LoginPage } from "@/pages/login-page";
import { RegisterPage } from "@/pages/register-page";

function renderWithProviders(ui: ReactElement, path = "/login") {
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[path]}>{ui}</MemoryRouter>
    </QueryClientProvider>,
  );
}

describe("LoginPage (auth shell check)", () => {
  it("renders the sign-in form", () => {
    renderWithProviders(<LoginPage />);

    expect(screen.getByRole("heading", { name: /sign in/i })).toBeInTheDocument();
    expect(screen.getByLabelText(/email/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/password/i)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /^sign in$/i })).toBeInTheDocument();
  });
});

describe("RegisterPage (form + MSW categories)", () => {
  it("renders the register form and reveals agent fields on toggle", async () => {
    const user = userEvent.setup();
    renderWithProviders(<RegisterPage />, "/register");

    expect(
      screen.getByRole("heading", { name: /create your account/i }),
    ).toBeInTheDocument();
    expect(screen.getByLabelText(/full name/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/email/i)).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: /create account/i }),
    ).toBeInTheDocument();

    // Toggle "Apply as an agent" — reveals the category selector
    // (fed by MSW /categories/public) and the optional message field.
    await user.click(screen.getByRole("switch", { name: /apply as an agent/i }));

    expect(await screen.findByLabelText(/work categories/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/message \(optional\)/i)).toBeInTheDocument();
  });
});

describe("MSW wiring", () => {
  it("intercepts the API base URL and serves fixtures", async () => {
    const response = await fetch(`${import.meta.env.VITE_API_BASE_URL}/api/categories/public`);
    expect(response.status).toBe(200);

    const body = (await response.json()) as Array<{ id: number; name: string }>;
    expect(body.length).toBeGreaterThan(0);
    expect(body[0]).toHaveProperty("name");
  });
});
