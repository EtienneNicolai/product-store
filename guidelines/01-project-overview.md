# Guideline 01 - Project Overview & Shared Contracts

## Goal
Build a small store where:
1. Anyone can browse a product catalog, no login required
2. A visitor's cart persists across page loads via a session cookie, no account needed
3. Checkout collects payment through Stripe's Payment Element, embedded in the Angular app,
   not a redirect to a Stripe-hosted page
4. A webhook confirms payment success server-side and only then marks the order paid and
   decrements stock
5. The visitor sees an order confirmation page after a successful payment

## Data Models
Defined in `backend/Store.Api/Models/`. All sessions import from here, do not redefine
elsewhere.

```csharp
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string Description { get; set; } = null!;
    public int PriceCents { get; set; }
    public string Currency { get; set; } = "aud";
    public string ImageUrl { get; set; } = null!;
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Cart
{
    public int Id { get; set; }
    public string SessionId { get; set; } = null!;   // from a cookie, not tied to a user account
    public DateTime CreatedAt { get; set; }
    public List<CartItem> Items { get; set; } = new();
}

public class CartItem
{
    public int Id { get; set; }
    public int CartId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public Product Product { get; set; } = null!;
}

public class Order
{
    public int Id { get; set; }
    public string StripePaymentIntentId { get; set; } = null!;
    public string Status { get; set; } = "pending";   // "pending" | "paid" | "failed"
    public int TotalCents { get; set; }
    public string CustomerEmail { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = null!;  // snapshot at time of purchase
    public int UnitPriceCents { get; set; }             // snapshot, price could change later
    public int Quantity { get; set; }
}
```

## API Contracts
All routes use the `/api/` prefix.

### Catalog (no auth)
**GET /api/products** -> list of active products
```json
[{"id": 1, "slug": "sticker-pack", "name": "Sticker Pack", "priceCents": 500, "currency": "aud", "imageUrl": "/img/stickers.png", "stockQuantity": 42}]
```

**GET /api/products/{slug}** -> full Product, `404` if not found or `isActive == false`

### Cart (session-cookie based, no auth)
**GET /api/cart** -> current cart (creates one and sets the session cookie if none exists)
```json
{"id": 1, "items": [{"id": 1, "productId": 1, "quantity": 2, "product": {"name": "Sticker Pack", "priceCents": 500}}]}
```

**POST /api/cart/items** -> `{"productId": 1, "quantity": 1}` -> updated Cart
Response `400` if `quantity > product.stockQuantity`

**PUT /api/cart/items/{id}** -> `{"quantity": 3}` -> updated Cart

**DELETE /api/cart/items/{id}** -> updated Cart

### Checkout (session-cookie based, no auth)
**POST /api/checkout/create-payment-intent** -> `{"email": "customer@example.com"}`
Response `200`: `{"clientSecret": "pi_..._secret_...", "totalCents": 1000}`
Creates a Stripe PaymentIntent for the current cart's total and a matching `Order` row with
`status = "pending"`. Does not decrement stock yet (see Key Trap 6 in CLAUDE.md).

**POST /api/checkout/webhook** -> raw Stripe event payload
No response body contract - this is Stripe calling us, not the frontend. On a verified
`payment_intent.succeeded` event: mark the matching Order `status = "paid"`, decrement stock
for each OrderItem, clear the cart.

**GET /api/orders/{id}** -> Order detail, used by the confirmation page
Response `404` if the order does not belong to the requesting session (compare via the
session cookie, not the order id alone - do not let a visitor guess another order's id and
view someone else's purchase).

## Session Boundaries
- **Session 1** owns: `Models/`, `Data/StoreDbContext.cs`, EF migrations, seed data
- **Session 2** owns: `Controllers/ProductsController.cs` - parallel with Session 3
- **Session 3** owns: `Controllers/CartController.cs`, `Controllers/CheckoutController.cs`,
  the Stripe SDK integration - parallel with Session 2
- **Session 4** owns: `Program.cs` - DI registration, CORS policy, the webhook route's raw-body
  middleware configuration, Swagger setup
- **Session 5** owns: `frontend/` - all Angular components, services, routing
- **Session 6** owns: `Store.Api.Tests/` - xUnit tests, mocking the Stripe client so tests do
  not make real API calls

**Sessions 2 and 3 can run in parallel.** Both depend only on Session 1. Session 3 (cart and
checkout) looks up `Product` rows directly through `StoreDbContext`, not by calling into
Session 2's controller - this keeps the two decoupled so they can be built and reviewed
independently.

Import chain (no circular imports allowed):
`Controllers -> Data -> Models`
Sessions must not import from each other's controllers.

## Dependencies

### Backend (NuGet)
| Package | Used for | Verify |
|---|---|---|
| `Microsoft.EntityFrameworkCore.Sqlite` | EF Core + SQLite provider (local dev - see CLAUDE.md Key Trap 7) | `dotnet list package` |
| `Microsoft.EntityFrameworkCore.SqlServer` | EF Core + SQL Server provider (added when a production migration set is generated) | `dotnet list package` |
| `Microsoft.EntityFrameworkCore.Tools` | Migrations | `dotnet ef --version` |
| `Stripe.net` | Stripe API client | `dotnet list package` |
| `Microsoft.AspNetCore.Cors` | CORS for the Angular origin | included in ASP.NET Core |
| `xunit`, `Moq` | Testing | `dotnet test` runs cleanly |

### Frontend (npm)
| Package | Used for |
|---|---|
| `@angular/core`, `@angular/router` | App framework, routing |
| `@stripe/stripe-js` | Loading Stripe.js and mounting the Payment Element |
| `@angular/common/http` | Calling the backend API |

## Phase 0 - Setup (must complete before any session writes code)

```powershell
# Backend
dotnet new webapi -n Store.Api -o backend/Store.Api
cd backend/Store.Api
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Stripe.net
dotnet new xunit -n Store.Api.Tests -o ../Store.Api.Tests

# Frontend
cd ../../frontend
ng new . --routing --style=scss --skip-git
npm install @stripe/stripe-js
```

Then:
1. Create a Stripe account (test mode) and note the publishable and secret keys
2. Set the secret key via `dotnet user-secrets` locally, never in `appsettings.json`
3. Set the publishable key in `frontend/src/environments/environment.ts`
4. Run `dotnet ef database update` and confirm the SQLite file gets created before any session
   begins (no LocalDB/SQL Server Express available in this environment - see CLAUDE.md Key
   Trap 7 for how production's SQL Server target is handled separately)
