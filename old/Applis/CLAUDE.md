# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

"CleanCity": a recipe-sharing site with role-based moderation. Users (`user` role) submit recipes,
which stay `pending` until an administrator (`admin` role) approves or rejects them from `/admin`.

## Stack

- `client/`: React 19 + Vite + TypeScript + React Router (no test runner configured)
- `server/`: Node.js + Express + TypeScript, PostgreSQL via `pg`, JWT in an httpOnly cookie, bcrypt for
  password hashing, zod for request validation (no test runner configured)

There is no test suite in either package — do not assume `npm test` exists.

## Commands

Run from `server/` or `client/` respectively (there is no root-level package.json).

```powershell
./scripts/start-db.ps1   # starts Postgres via Docker (idempotent, persistent volume)
./scripts/run-dev.ps1    # installs deps if needed, runs server (:4000) + client (:5173)
```

Server (`server/`):
- `npm run dev` — tsx watch, http://localhost:4000
- `npm run build` — `tsc -p tsconfig.json`
- `npm start` — run compiled `dist/index.js`
- `npm run migrate` — apply `src/db/migrate.ts` (idempotent `CREATE TABLE IF NOT EXISTS` / `ADD COLUMN IF NOT EXISTS`)
- `npm run seed` — create the admin account from `.env` (`SEED_ADMIN_*`)

Client (`client/`):
- `npm run dev` — Vite dev server, http://localhost:5173
- `npm run build` — `tsc -b && vite build`
- `npm run lint` — oxlint
- `npm run preview` — preview production build

Both need `.env` copied from `.env.example` before running (`server/.env`: `DATABASE_URL`, `JWT_SECRET`,
`SEED_ADMIN_*`, `CLIENT_ORIGIN`; `client/.env`: `VITE_API_URL`).

VS Code debugging: open `CleanCity.code-workspace`, then use the Run and Debug configs ("Server: Debug
(Express/TS)", "Client: Debug (Chrome)", "Full Stack: Server + Client") defined in `.vscode/launch.json`
and `.vscode/tasks.json`.

## Architecture

### Auth

- Login is by **username** (`pseudo`), not email, despite `memo.txt` listing email-style test creds.
- `server/src/utils/jwt.ts` signs/verifies a JWT (`{ id, role }`, 7d expiry) using `JWT_SECRET` (throws at
  import time if unset).
- `server/src/middleware/auth.ts`: `requireAuth` reads the `token` httpOnly cookie and populates
  `req.user`; `requireRole("admin"|"user")` gates by role. Chain them: `requireAuth, requireRole("admin")`.
- Cookie is set/cleared in `server/src/routes/auth.ts` (`COOKIE_OPTIONS`: httpOnly, sameSite=lax,
  secure only in production).
- Client: `client/src/context/AuthContext.tsx` holds the current user, calling `GET /api/auth/me` on
  mount to hydrate session state from the cookie. `client/src/components/ProtectedRoute.tsx` exposes
  `ProtectedRoute` (any authenticated user) and `AdminRoute` (admin only), used in `App.tsx` routing.
- `client/src/api/client.ts` (`apiFetch`) always sends `credentials: "include"` and throws `ApiError` on
  non-2xx responses — all API modules (`api/auth.ts`, `api/recipes.ts`) build on this.

### Server routing / data model

- Route mounting in `server/src/index.ts`: `/api/auth`, `/api/recipes`, `/api/admin` — each a router
  in `server/src/routes/`. A catch-all error handler returns a generic 500 JSON body.
- `server/src/db/pool.ts` exports a single shared `pg.Pool`; every route queries it directly (no ORM,
  no repository layer).
- Schema (`server/src/db/migrate.ts`, run manually via `npm run migrate`, not auto-run):
  - `users`: `username` (unique, login key), `first_name`, `last_name`, `email` (unique), `phone`,
    `password_hash`, `role` (`admin`|`user`).
  - `recipes`: `title`, `description`, `ingredients`, `steps`, `author_id` (FK → users, cascade delete),
    `status` (`pending`|`approved`|`rejected`), `reviewed_at`, `reviewed_by` (FK → users, set null).
  - The migration file also contains one-time `ALTER TABLE ... ADD COLUMN IF NOT EXISTS` / backfill
    statements for columns added after initial release — follow that additive pattern for future schema
    changes rather than editing the original `CREATE TABLE` in a breaking way.
- Recipe visibility rules (`server/src/routes/recipes.ts`): `GET /api/recipes` only returns `approved`
  recipes; `GET /api/recipes/:id` returns 404 for non-approved recipes unless the requester is the
  author or an admin. Editing/deleting a recipe is only allowed by its author while `status = 'pending'`.
- Admin moderation (`server/src/routes/admin.ts`) is mounted behind `requireAuth, requireRole("admin")`
  for the whole router; approve/reject only affect rows still in `pending` status (`WHERE ... AND status
  = 'pending'` guards against double-processing).

### Client structure

- `client/src/pages/`: one component per route (`HomePage`, `LoginPage`, `RegisterPage`,
  `MyRecipesPage`, `RecipeDetailPage`, `AdminPage`) wired up in `client/src/App.tsx`.
- `client/src/components/Layout.tsx` provides the shared shell (via `<Route element={<Layout />}>` +
  nested routes/`<Outlet>`).
- French is the UI/error-message language throughout (validation messages, API error strings) — match
  it when adding new user-facing text or server error responses.
