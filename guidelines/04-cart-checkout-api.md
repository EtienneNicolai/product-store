# Guideline 04 - Cart & Checkout API

## Goal
Cart management and the actual Stripe integration. This is the session the whole project
exists to practice - take the time to get the webhook handling right.

## Owns
- `Controllers/CartController.cs` - the four cart endpoints from guideline 01
- `Controllers/CheckoutController.cs` - create-payment-intent and the webhook receiver
- The Stripe.NET client setup (where the secret key is read from, see CLAUDE.md Key Trap 5)

## Requirements
- Every cart endpoint reads/creates the cart from the session cookie - if no cookie exists,
  `GET /api/cart` creates a new Cart row and sets one before returning
- `POST /api/checkout/create-payment-intent` computes the total server-side from the cart's
  current items and current product prices - never trust a total sent from the client
- Create the matching `Order` row (status `pending`) in the same action that creates the
  PaymentIntent, storing `StripePaymentIntentId` so the webhook can find it later
- The webhook action must verify the Stripe signature using the raw request body (CLAUDE.md
  Key Trap 2) before trusting the event at all
- On a verified `payment_intent.succeeded` event: look up the Order by
  `StripePaymentIntentId`, set `status = "paid"`, decrement `StockQuantity` on each ordered
  Product, then clear the cart's items. Do all of this in one transaction.
- Look up product prices directly via `StoreDbContext`, not by calling `ProductsController` -
  this is what keeps this session decoupled from Session for the catalog API (see guideline 01,
  Session Boundaries)

## Depends on
Session 1 (data layer) only. Runs in parallel with the catalog API session - does not import
anything from `ProductsController`.

## Completion notes
- Session cookie handling is a plain opaque cookie (`session_id`, a GUID), read/set directly via
  `HttpContext.Request/Response.Cookies` in a small shared `CartSession`/`CartAccessor` helper -
  not ASP.NET session-state middleware. Every cart and checkout endpoint goes through the same
  get-or-create helper, not just `GET /api/cart` as the guideline states literally - a visitor
  who calls `POST /api/cart/items` first without ever calling `GET /api/cart` still needs a
  session.
- Added `Order.SessionId` (see guideline 01) with its own migration, since `GET /api/orders/{id}`
  has nothing else to compare the requesting cookie against.
- The webhook handler no-ops on an already-`"paid"` order before doing anything else, to guard
  against Stripe's documented at-least-once event redelivery - not explicitly required by this
  guideline, but a real correctness gap without it (a redelivered event would double-decrement
  stock).
- The Stripe secret key and webhook secret are read via `IConfiguration["Stripe:..."]` at the
  point of use in the controllers, not as a `Program.cs` global assignment - keeps this session's
  changes out of Session 4's file. Both are unset in this environment (no Stripe account exists
  yet); `create-payment-intent` fails with a clear 500 "Stripe not configured" response rather
  than crashing, and the webhook does the same for a missing webhook secret. Verified live:
  cart endpoints and the webhook's signature verification (both accept and reject, using
  Stripe.net's real HMAC scheme via a manually-inserted pending order) work end to end without
  any real Stripe credentials - `create-payment-intent` itself cannot be live-verified until a
  real test-mode secret key exists.
