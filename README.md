# Product Store

A small e-commerce store: product catalog, cart, and a real Stripe checkout (Payment Element,
embedded in the app - not a redirect to a Stripe-hosted page). Built to close a specific set of
skill gaps: real .NET/EF depth, Angular, and a genuine payment integration, not a fake "add to
cart" button.

**Status:** All 6 build sessions complete - catalog, cart, checkout, API wiring, Angular
frontend, and automated tests. Not deployed anywhere yet, and checkout can't be exercised fully
end to end until a real Stripe test-mode account is connected (see `CLAUDE.md`). See
`guidelines/` for the full session-by-session build plan and what actually changed along the
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
- **Database:** SQLite locally, SQL Server / Azure SQL in production (see `CLAUDE.md` Key Trap 7
  for how the provider switch is handled)
- **Payments:** Stripe.NET SDK (Payment Intents API)
- **Frontend:** Angular
- **Auth:** none in v1 - carts are anonymous, keyed by a session cookie

## Getting started

```powershell
# Backend
cd backend/Store.Api
dotnet ef database update
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

## Development workflow

- Backlog lives in GitHub Issues.
- One branch per feature or fix. Branch from `master`.
- Parallel work uses git worktrees when more than one session is in flight at once.
- See `CLAUDE.md` and `guidelines/` for the full architecture and session plan.

## Known issues / roadmap

See `guidelines/` for the full session-by-session build plan.
