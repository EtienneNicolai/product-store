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

## Completion notes
- CORS origins are read from `Cors:AllowedOrigins` in configuration (defaulting to
  `http://localhost:4200` if unset) rather than hardcoded, so a deployed frontend origin can be
  added later via `appsettings.Production.json` or an environment variable without a code
  change.
- The webhook's "different request body handling" turned out to need no middleware at all -
  `CheckoutController.HandleWebhook` (Session 3) never binds a `[FromBody]` DTO, it reads
  `Request.Body` directly inside the action, which is naturally scoped to that one route with
  nothing global required. Nothing to add here beyond what Session 3 already built.
- Verified live, actually cross-origin (not just same-origin curl): a CORS preflight
  (`OPTIONS` with `Origin: http://localhost:4200`) returns `Access-Control-Allow-Credentials:
  true` and the exact origin; an actual `GET`/`POST` with that Origin header round-trips the
  `session_id` cookie correctly (added an item on one request, confirmed it was still in the
  cart on a second request using the same cookie); a request from an origin NOT on the allowlist
  gets no `Access-Control-*` headers at all.
- Swagger-in-development-only looked broken when tested via `export ASPNETCORE_ENVIRONMENT=
  Production; dotnet run` - it still reported `Hosting environment: Development`. That's not a
  bug: `Properties/launchSettings.json`'s `http` profile hardcodes `ASPNETCORE_ENVIRONMENT=
  Development` and `dotnet run` uses it by default, overriding the shell's env var.
  `launchSettings.json` is a local-dev-only file, never used by an actual deployment. Confirmed
  correctly by running the compiled DLL directly (`dotnet bin/Debug/net8.0/Store.Api.dll`) with
  `ASPNETCORE_ENVIRONMENT=Production` set - Swagger correctly 404s, the API itself still works.
