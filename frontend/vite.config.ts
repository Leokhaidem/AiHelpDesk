import { fileURLToPath } from "node:url";
import path from "node:path";

import tailwindcss from "@tailwindcss/vite";
import react from "@vitejs/plugin-react";
import { defineConfig } from "vite";

const dirname = path.dirname(fileURLToPath(import.meta.url));

// The dev server MUST run on port 5173: the backend CORS default only
// allows http://localhost:5173 (see docs/frontend-integration.md, section 2).
export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      "@": path.resolve(dirname, "./src"),
    },
  },
  server: {
    port: 5173,
    strictPort: true,
  },
});
