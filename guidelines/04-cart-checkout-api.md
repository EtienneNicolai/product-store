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
