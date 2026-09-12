# Product Store

A small e-commerce store: product catalog, cart, and a real Stripe checkout (Payment Element,
embedded in the app - not a redirect to a Stripe-hosted page). Built to close a specific set of
skill gaps: real .NET/EF depth, Angular, and a genuine payment integration, not a fake "add to
cart" button.

**Status:** All 6 build sessions complete - catalog, cart, checkout, API wiring, Angular
frontend, and automated tests. Deploying to Render (see Deployment below); checkout can't be
exercised fully end to end until a real Stripe test-mode account is connected (see `CLAUDE.md`).
See `guidelines/` for the full session-by-session build plan and what actually changed along the
way.

## What it does

- Anyone can browse a product catalog, no login required
- A visitor's cart persists across page loads via a session cookie, no account needed
- Checkout collects payment through Stripe's Payment Element
- A webhook confirms payment success server-side and only then marks the order paid and
  decrements stock
- An order confirmation page after a successful payment

## Stack

- **Backend:** ASP.NET Core Web API (.NET 8) + Entity Framework Core
- **Database:** SQLite locally, Postgres in production (see `CLAUDE.md` Key Trap 7 for how the
  provider switch is handled, and Deployment below for why Postgres/Render rather than the
  originally planned SQL Server/Azure SQL)
- **Payments:** Stripe.NET SDK (Payment Intents API)
- **Frontend:** Angular
- **Auth:** none in v1 - carts are anonymous, keyed by a session cookie
- **Deployment:** Render

## Getting started

```powershell
# Backend - creates its own local SQLite database on startup, no separate
# migration step needed
cd backend/Store.Api
dotnet run

# Frontend (separate terminal)
cd frontend
npm install --legacy-peer-deps
ng serve
```

Backend on http://localhost:5000 (or whatever `launchSettings.json` assigns), frontend on
http://localhost:4200. `--legacy-peer-deps` is required here - see `CLAUDE.md` Key Trap 8.

Requires a Stripe account (test mode) - see `CLAUDE.md` for how keys are configured.

## Running tests

```powershell
# Backend
cd backend/Store.Api.Tests
dotnet test

# Frontend
cd frontend
ng test --watch=false
```

Backend tests never hit a real database or make a real Stripe API call - see `CLAUDE.md` and
`guidelines/07-testing.md` for how.

## Deployment

Deployed to Render, not Azure - the originally planned SQL Server/Azure SQL target was dropped
because Azure's account signup (card, phone/identity verification) was too much friction for a
personal demo project. This is a genuine change to the plan, not just a hosting detail: the
backend now targets Postgres in production instead of SQL Server (see `CLAUDE.md` Key Trap 7).

Three separate resources, in this order:

**1. PostgreSQL** (New + -> PostgreSQL) - note the internal connection string it gives you,
needed by the Web Service below.

**2. Web Service** (New + -> Web Service, connect this repo) - Render has no native .NET
runtime, so this must build via the Dockerfile:
| Setting | Value |
|---|---|
| Root Directory | `backend/Store.Api` |
| Environment | Docker |
| Dockerfile Path | `Dockerfile` |
| Branch | `master` |
| Instance Type | Free |
| Auto-Deploy | Yes |

Environment variables on this service:
| Variable | Value |
|---|---|
| `Database__Provider` | `Postgres` |
| `ConnectionStrings__StoreDb` | the Postgres connection string from step 1 |
| `Stripe__SecretKey` | Stripe test-mode secret key |
| `Stripe__WebhookSecret` | Stripe webhook signing secret |
| `Cors__AllowedOrigins__0` | the Static Site's URL from step 3, once known |

**3. Static Site** (New + -> Static Site, same repo):
| Setting | Value |
|---|---|
| Root Directory | `frontend` |
| Build Command | `npm install --legacy-peer-deps && npm run build -- --configuration production` |
| Publish Directory | `dist/frontend/browser` |

`frontend/src/environments/environment.prod.ts` assumes the Web Service is named
`product-store-api` (Render URLs are `https://<service-name>.onrender.com`, deterministic from
the name chosen at creation) - update it if a different name was used.

## Development workflow

- Backlog lives in GitHub Issues.
- One branch per feature or fix. Branch from `master`.
- Parallel work uses git worktrees when more than one session is in flight at once.
- See `CLAUDE.md` and `guidelines/` for the full architecture and session plan.

## Known issues / roadmap

See `guidelines/` for the full session-by-session build plan.
