# Guideline 06 - Angular Frontend

## Goal
The full customer-facing app.

## Owns
- `frontend/src/app/products/` - listing and detail views
- `frontend/src/app/cart/` - cart view (quantity edit, remove)
- `frontend/src/app/checkout/` - collects email, calls create-payment-intent, mounts Stripe's
  Payment Element using the returned `clientSecret`, confirms payment client-side via
  `stripe.confirmPayment()`
- `frontend/src/app/order-confirmation/` - polls or fetches `GET /api/orders/{id}` after
  redirect back from Stripe, shows order status
- An `ApiService` (or similar) wrapping all HTTP calls to the backend, so no component calls
  `HttpClient` directly

## Requirements
- All API calls must send credentials (`withCredentials: true`) so the session cookie the
  cart depends on is actually sent and received
- The publishable Stripe key comes from `environment.ts`, never hardcoded in a component
- Handle the case where `stripe.confirmPayment()` requires a redirect (3D Secure) - the
  Payment Element's default behaviour handles this, do not fight it by trying to keep the
  customer on the checkout page unconditionally
- Show real loading and error states on every network call - a store with no visible feedback
  during checkout is worse than one with slightly ugly feedback

## Depends on
Sessions 1-4 (the full backend must exist and be running for this session's API calls to have
anything to talk to).
