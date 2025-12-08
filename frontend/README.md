# Frontend (React) Scaffold

Planned stack:
- Vite + React + TypeScript
- State/data: React Query for server state; light client state with Zustand/Redux Toolkit if needed.
- UI: Choose component library (e.g., Mantine or MUI) + theming; target responsive layout.
- Build: `npm create vite@latest frontend -- --template react-ts` (run inside repo root).

Initial steps to flesh out:
1) Init Vite project in `frontend/` with package.json, tsconfig, eslint/prettier.
2) Configure base API client with env-driven `VITE_API_BASE`.
3) Add routes/pages: Dashboard, Blueprint search, Manufacturing calc, Prices, Shopping list.
4) Add shared components: layout shell, data tables, forms, toasts, loading states.
5) Set up testing: Vitest + React Testing Library; Playwright/Cypress for E2E later.
6) Add CI jobs for lint/test/build; emit static assets for deployment.
