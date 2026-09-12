# Product Store - Agent Instructions

## What is this project
A small e-commerce store: product catalog, cart, and a real Stripe checkout, no fake "add to
cart" button. Anonymous carts (no user accounts in v1), session-cookie based. Built to close a
specific set of skill gaps: real .NET/EF depth, Angular, and a genuine payment integration.

## Tech Stack
- **Backend**: ASP.NET Core Web API (.NET 8) + Entity Framework Core
- **Database**: SQLite locally (file-based, zero install), Postgres in production - see Key
  Trap 7 for how migrations handle the provider switch, and README for why production ended up
  on Postgres/Render rather than the originally planned SQL Server/Azure SQL
- **Payments**: Stripe.NET SDK (Payment Intents API, not the older Checkout redirect, so the
  Angular app owns the actual checkout UI)
- **Frontend**: Angular (latest stable)
- **Auth**: none in v1 - carts are anonymous, keyed by a session cookie
- **Deployment**: Render - backend as a Docker-based Web Service (Render has no native .NET
  runtime), frontend as a Static Site, Postgres as a managed database

## Folder layout
```
backend/
  Store.Api/
    Models/         - Product, Cart, CartItem, Order, OrderItem
    Data/           - StoreDbContext
    Migrations/      - EF Core migrations (default `dotnet ef` output location, not under Data/)
    Controllers/     - ProductsController, CartController, CheckoutController,
                       CartSessionSupport (shared session-cookie helper)
    Dtos/             - request/response shapes for cart and checkout endpoints
    Services/          - IPaymentIntentGateway (Stripe.net wrapper, makes checkout
                         testable - see guideline 07's completion notes)
    Program.cs        - DI, CORS, middleware, Stripe webhook raw-body config
                        (also: public partial class Program {} at the bottom, for
                        WebApplicationFactory<Program> in tests)
  Store.Api.Tests/    - xUnit + Moq + WebApplicationFactory integration tests,
                        in-memory SQLite (not the dev store.db, never real Stripe)
frontend/
  src/app/
    products/          - listing + detail
    cart/               - cart view
    checkout/            - Stripe Payment Element integration
    order-confirmation/   - post-payment confirmation page, polls order status
    services/             - ApiService (all HTTP calls), models, error/format helpers
  src/environments/       - environment.ts / environment.prod.ts (not scaffolded by
                            default in Angular 21 - added manually with angular.json
                            fileReplacements, see guideline 06's completion notes)
guidelines/            - session-by-session implementation guides
```

## Session map
| Session | Guideline | What it builds |
|---|---|---|
| 1 | 02-data-layer.md | EF models, DbContext, migrations, seed data |
| 2 | 03-catalog-api.md | ProductsController (parallel with 3) |
| 3 | 04-cart-checkout-api.md | CartController, CheckoutController, Stripe integration (parallel with 2) |
| 4 | 05-api-wiring.md | Program.cs, CORS, Stripe webhook middleware, Swagger |
| 5 | 06-angular-frontend.md | Full Angular app |
| 6 | 07-testing.md | xUnit backend tests, Angular component tests |

Sessions 2 and 3 can run in parallel - they depend only on Session 1 and do not import from
each other. The cart/checkout session looks up product prices directly via the DbContext, not
by calling into the catalog controller.

## Key traps (read before writing any code)
1. **Store money as an integer number of cents, not `decimal` dollars.** Stripe's own API works
   in the smallest currency unit. Converting back and forth between dollars and cents at every
   boundary is where rounding bugs come from - store cents everywhere, format to dollars only
   at render time.
2. **The Stripe webhook endpoint must read the raw request body for signature verification.**
   ASP.NET's default JSON model binding will consume the body before `Stripe.EventUtility` can
   verify the signature against it. The webhook action needs to read `Request.Body` directly as
   a stream/string, not bind a DTO parameter.
3. **CORS must explicitly allow the Angular dev server origin** (`http://localhost:4200` by
   default) or every API call from the Angular app fails silently as a CORS error in the
   browser console during local development.
4. **EF Core needs explicit precision/scale on any decimal column** (`OnModelCreating`, e.g.
   `.HasPrecision(18, 2)`) - without it, some database providers silently apply a default that
   truncates values.
5. **Never send the Stripe secret key to the Angular frontend.** Only the publishable key goes
   client-side (in Angular environment config). The secret key lives server-side only, in user
   secrets locally and a proper secret store in production, never committed to the repo.
6. **Decrement stock only after the webhook confirms payment succeeded**, not when the
   PaymentIntent is first created. Creating a PaymentIntent does not mean the customer has paid.
7. **EF Core migrations are provider-specific - a SQLite migration will not apply to Postgres
   (or SQL Server).** Local dev targets SQLite (no LocalDB/SQL Server Express install available)
   and production targets Postgres on Render (Azure's signup friction ruled it out - see
   guideline 01 and README for why the plan changed from the original SQL Server/Azure SQL
   target). Two providers, same `StoreDbContext`/`OnModelCreating`, but genuinely different
   migration *mechanisms*, not just different SQL: SQLite has no checked-in migrations at all -
   `Program.cs` calls `Database.EnsureCreated()` for it, since the local `store.db` is disposable
   dev data with no history worth versioning. Postgres has a real migration set
   (`Migrations/`, generated with `Database:Provider=Postgres` set, targeting `Npgsql.
   EntityFrameworkCore.PostgreSQL`) applied via `Database.Migrate()` at startup - both paths run
   automatically when the app starts, no separate manual step needed either locally or on
   Render. Money-as-cents (Key Trap 1) already avoids the worst cross-provider pitfall (decimal
   precision), which is most of why running two providers off one model is workable at all.
   If a third provider ever needs its own migration set, don't try to add it to this same
   `Migrations/` folder - a single `DbContext` can only have one properly-recognised model
   snapshot in one assembly; Postgres's is already what lives there.
8. **Use `@angular/cli@21` (the `v21-lts` dist-tag), not `@latest`.** `@latest` resolves to a
   version requiring a newer Node than what's installed here; `21` is the current LTS and works
   fine with the installed Node 22.19. Separately, `npm install` in `frontend/` needs
   `--legacy-peer-deps` - npm 10.9.3 has an arborist bug (`Cannot read properties of null
   (reading 'edgesOut')`) on this Angular version's `vitest`/`@vitest/browser-playwright` peer
   dependency graph. The scaffolded app uses Vitest (not Karma/Jasmine) as its test runner -
   relevant for Session 6's Angular component tests.
9. **`CheckoutController` depends on `IPaymentIntentGateway` (`Services/`), not Stripe.net's
   `PaymentIntentService` directly.** This Stripe.net version (52.4.2) has no
   `IPaymentIntentService` interface to mock, so the project owns a thin wrapper interface
   instead. If you're touching checkout code: don't replace this with
   `new PaymentIntentService(new StripeClient(secretKey))` inline again - that's exactly what
   makes `Store.Api.Tests` unable to test it without a real network call to Stripe.

## Running the app locally
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

## Running tests
```powershell
# Backend
cd backend/Store.Api.Tests
dotnet test

# Frontend
cd frontend
ng test --watch=false
```

## Stripe local testing
Use the Stripe CLI to forward webhook events to localhost during development:
```powershell
stripe listen --forward-to localhost:5000/api/checkout/webhook
```
Use Stripe's published test card numbers (e.g. 4242 4242 4242 4242) - never use a real card
against the test-mode keys.
