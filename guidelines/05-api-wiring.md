# Guideline 05 - API Wiring

## Goal
Wire everything built in Sessions 1-3 into a running application.

## Owns
- `Program.cs` - DI registration for `StoreDbContext`, controller registration, CORS policy,
  Swagger/OpenAPI setup
- The raw-body middleware configuration specifically for the webhook route (CLAUDE.md Key
  Trap 2) - the webhook endpoint needs different request body handling than every other
  controller action, this has to be scoped to that one route, not applied globally

## Requirements
- CORS policy must explicitly allow `http://localhost:4200` (and whatever the deployed
  frontend origin becomes later) - do not use `AllowAnyOrigin` combined with credentials,
  the two are mutually exclusive in ASP.NET Core and cookies (which the cart depends on)
  require credentialed CORS
- The session cookie used for cart identification needs `SameSite` and `Secure` settings that
  actually work with the Angular dev server running on a different port than the API - test
  this explicitly, it is a common silent failure point
- Swagger should be enabled in development only

## Depends on
Sessions 1, 2 and 3 (data layer, catalog API, cart/checkout API) all need to exist first, since
this session wires them together rather than building new endpoints.
