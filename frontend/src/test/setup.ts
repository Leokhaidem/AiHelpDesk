import "@testing-library/jest-dom/vitest";
import { cleanup } from "@testing-library/react";
import { afterAll, afterEach, beforeAll } from "vitest";

import { mswServer } from "./msw";

// jsdom does not implement ResizeObserver; Radix primitives (popover,
// command, dropdown…) use it via @radix-ui/react-use-size.
class ResizeObserverStub implements ResizeObserver {
  observe(): void {}
  unobserve(): void {}
  disconnect(): void {}
}

Object.defineProperty(window, "ResizeObserver", {
  writable: true,
  value: ResizeObserverStub,
});

beforeAll(() => {
  mswServer.listen({ onUnhandledRequest: "error" });
});

afterEach(() => {
  // Vitest runs without `globals: true`, so @testing-library/react cannot
  // register its automatic cleanup — do it explicitly.
  cleanup();
  mswServer.resetHandlers();
});

afterAll(() => {
  mswServer.close();
});
