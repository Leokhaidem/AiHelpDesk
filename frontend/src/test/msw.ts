import { http, HttpResponse } from "msw";
import { setupServer } from "msw/node";

/**
 * MSW handlers mirror the backend API contract in docs/frontend-integration.md.
 * URLs are absolute (based on VITE_API_BASE_URL) so they match the axios baseURL
 * used in tests.
 */
const api = import.meta.env.VITE_API_BASE_URL;

/** All backend routes live under the /api prefix (see CategoriesController etc.). */
export const publicCategoriesUrl = `${api}/api/categories/public`;

export const publicCategories = [
  { id: 1, name: "General", description: "General support questions" },
  { id: 2, name: "Billing", description: "Billing and invoicing questions" },
  { id: 3, name: "Access", description: "Account access and permissions" },
];

export const mswServer = setupServer(
  http.get(publicCategoriesUrl, () => {
    return HttpResponse.json(publicCategories);
  }),
);
