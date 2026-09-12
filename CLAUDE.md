# Product Store - Agent Instructions

## What is this project
A small e-commerce store: product catalog, cart, and a real Stripe checkout, no fake "add to
cart" button. Anonymous carts (no user accounts in v1), session-cookie based. Built to close a
specific set of skill gaps: real .NET/EF depth, Angular, and a genuine payment integration.

## Tech Stack
- **Backend**: ASP.NET Core Web API (.NET 8) + Entity Framework Core
- **Database**: SQLite locally (file-based, zero install), SQL Server / Azure SQL in
  production - see Key Trap 7 for how migrations handle the provider switch
- **Payments**: Stripe.NET SDK (Payment Intents API, not the older Checkout redirect, so the
  Angular app owns the actual checkout UI)
- **Frontend**: Angular (latest stable)
- **Auth**: none in v1 - carts are anonymous, keyed by a session cookie

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
    Program.cs        - DI, CORS, middleware, Stripe webhook raw-body config
  Store.Api.Tests/    - xUnit tests
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
7. **EF Core migrations are provider-specific - a SQLite migration will not apply to SQL
   Server.** Local dev targets SQLite (no LocalDB/SQL Server Express install available), but
   production targets SQL Server/Azure SQL as originally planned. The model classes and
   `OnModelCreating` config in `StoreDbContext` are the single source of truth; when it's time
   to deploy, generate a *separate* migration set targeting `Microsoft.EntityFrameworkCore.
   SqlServer` rather than trying to reuse the SQLite one. Money-as-cents (Key Trap 1) already
   avoids the worst cross-provider pitfall (decimal precision), which is most of why this
   two-provider setup is workable at all.
8. **Use `@angular/cli@21` (the `v21-lts` dist-tag), not `@latest`.** `@latest` resolves to a
   version requiring a newer Node than what's installed here; `21` is the current LTS and works
   fine with the installed Node 22.19. Separately, `npm install` in `frontend/` needs
   `--legacy-peer-deps` - npm 10.9.3 has an arborist bug (`Cannot read properties of null
   (reading 'edgesOut')`) on this Angular version's `vitest`/`@vitest/browser-playwright` peer
   dependency graph. The scaffolded app uses Vitest (not Karma/Jasmine) as its test runner -
   relevant for Session 6's Angular component tests.

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
cd backend/Store.Api.Tests
dotnet test
```

## Stripe local testing
Use the Stripe CLI to forward webhook events to localhost during development:
```powershell
stripe listen --forward-to localhost:5000/api/checkout/webhook
```
Use Stripe's published test card numbers (e.g. 4242 4242 4242 4242) - never use a real card
against the test-mode keys.
