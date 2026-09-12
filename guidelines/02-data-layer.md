# Guideline 02 - Data Layer

## Goal
The EF models, DbContext and first migration. No controllers in this session.

## Owns
- `Models/Product.cs`, `Cart.cs`, `CartItem.cs`, `Order.cs`, `OrderItem.cs` (shapes defined in
  guideline 01)
- `Data/StoreDbContext.cs` - `DbSet<T>` for each model, `OnModelCreating` with explicit decimal
  precision if any money field ends up as decimal anywhere (it should not - see CLAUDE.md Key
  Trap 1, everything money-related is `int` cents)
- Initial EF migration
- Seed data: at least 5 real or placeholder products with images, prices and stock

## Requirements
- Foreign key relationships must cascade delete appropriately (deleting a Cart deletes its
  CartItems, deleting an Order deletes its OrderItems) but must NOT cascade delete a Product
  when a CartItem or OrderItem references it - `OrderItem` already snapshots `ProductName` and
  `UnitPriceCents` specifically so historical orders survive a product being deleted or
  changed later
- `Cart.SessionId` should be indexed - it is how every cart lookup happens

## Depends on
Nothing. This is the first session.

## Completion notes
- The `dotnet new webapi` scaffold defaults to minimal APIs (a `WeatherForecast` endpoint, no
  `Controllers/` folder), but the plan assumes MVC controllers. Fixed as part of this session:
  `Program.cs` now has `AddControllers()`/`MapControllers()` and the WeatherForecast template
  code is gone. Session 2/3 can add files straight into `Controllers/`.
- `Program.cs` also got a minimal `AddDbContext<StoreDbContext>` registration (reading
  `ConnectionStrings:StoreDb` from `appsettings.json`, currently `Data Source=store.db`) since
  `dotnet ef` needs the context registered to run migrations at all. Session 4 builds CORS/
  webhook/Swagger config around this, not instead of it.
- Seed data is 6 products, not 5 - the 6th (`discontinued-keychain`) is deliberately
  `IsActive = false` / `StockQuantity = 0` so Session 2's "list active products" and "404 an
  inactive product" behavior (guideline 01's API contract) has a real row to test against, not
  just an empty case.
- Product images use `https://picsum.photos/seed/<slug>/600/400` placeholders (a real,
  resolvable placeholder image service) rather than local paths that would 404 - same reasoning
  as the portfolio site's `heroImage` handling, don't reference an asset that doesn't exist.
- `dotnet-ef` (global tool) installed at version 10.0.12 against this project's EF Core 8.x
  packages - works fine despite the version gap, migrations generate and apply cleanly.
