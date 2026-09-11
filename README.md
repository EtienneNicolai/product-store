# Product Store

A small e-commerce store: product catalog, cart, and a real Stripe checkout (Payment Element,
embedded in the app - not a redirect to a Stripe-hosted page). Built to close a specific set of
skill gaps: real .NET/EF depth, Angular, and a genuine payment integration, not a fake "add to
cart" button.

**Status:** In progress. See `guidelines/` for the full session-by-session build plan.

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
npm install
ng serve
```

Backend on http://localhost:5000 (or whatever `launchSettings.json` assigns), frontend on
http://localhost:4200.

Requires a Stripe account (test mode) - see `CLAUDE.md` for how keys are configured.

## Development workflow

- Backlog lives in GitHub Issues.
- One branch per feature or fix. Branch from `master`.
- Parallel work uses git worktrees when more than one session is in flight at once.
- See `CLAUDE.md` and `guidelines/` for the full architecture and session plan.

## Known issues / roadmap

See `guidelines/` for the full session-by-session build plan.
