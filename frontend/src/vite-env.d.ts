/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** Base URL of the AI Help Desk API, e.g. https://localhost:7124 */
  readonly VITE_API_BASE_URL: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
