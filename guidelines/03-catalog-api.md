# Guideline 03 - Catalog API

## Goal
Read-only product endpoints. See guideline 01 for the exact contract.

## Owns
- `Controllers/ProductsController.cs` - `GET /api/products`, `GET /api/products/{slug}`

## Requirements
- `GET /api/products` only ever returns rows where `IsActive == true`
- `GET /api/products/{slug}` returns `404` for both a non-existent slug and an inactive
  product - do not leak the existence of an inactive product via a different error shape
- Query directly through `StoreDbContext`, do not add a separate repository/service
  abstraction for v1 - it is not needed at this scale and adds indirection this project does
  not benefit from

## Depends on
Session 1 (data layer) only. Runs in parallel with Session for cart/checkout - does not import
anything from `CartController` or `CheckoutController`.
