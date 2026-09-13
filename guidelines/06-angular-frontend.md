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

## Completion notes
- `CreatePaymentIntentResponse` (Session 3/4's contract) gained an `OrderId` field - the frontend
  needs it to build `return_url: /order-confirmation/{orderId}` for Stripe's redirect and to
  know which order to poll afterward. Not in guideline 01's original contract; documented there
  too.
- Cart item lines don't carry a `currency` field (`ProductSummaryDto` is just `{name,
  priceCents}`), so cart/checkout/order-confirmation price formatting hardcodes `'aud'` rather
  than reading it per item. This matches the backend's own assumption
  (`CheckoutController.CreatePaymentIntent` already takes `cart.Items.First().Product.Currency`
  as the whole cart's currency) - a single-currency cart is the actual v1 design, not an
  oversight.
- Angular 21's `ng new` no longer scaffolds `src/environments/` by default - added
  `environment.ts`/`environment.prod.ts` manually plus the `fileReplacements` config in
  `angular.json` (also not scaffolded by default) so the production build actually swaps them.
- `stripe.confirmPayment()` is called with no `redirect` override, so Stripe's own default
  (`'always'`) decides whether to redirect - per this guideline, not fought against. The
  Payment Element's mount target only exists once Angular renders the 'payment' stage, so
  mounting uses `afterNextRender` (Angular's own post-render hook) rather than a raw
  `setTimeout`, to avoid a race between Stripe.js and Angular's own change detection.
- Verified for real in a headless Chromium browser (no project run-skill existed yet, used
  Playwright directly against the running dev server + API): product list renders 5 real
  products with real images, add-to-cart works, cart shows the correct item/price/total,
  checkout's email step correctly surfaces the backend's actual "Stripe not configured" error
  rather than crashing or showing a blank page - confirmed both via console output and
  screenshots. This is the expected, honest state until a real Stripe account exists; the code
  path itself (create-payment-intent -> mount Payment Element -> confirmPayment) is real and
  wired correctly, just untestable end-to-end without real keys.
- **"Comfortable" visual redesign** (post-launch, once the catalog grew to 10 products): warm
  cream/terracotta palette, Fraunces (serif) for headings + Nunito (rounded sans) for body,
  defined as CSS custom properties in `styles.scss`. The Payment Element itself is themed to
  match via Stripe's Appearance API (`checkout.ts`'s `STRIPE_APPEARANCE` constant) - CSS alone
  can't reach into Stripe's iframe, so the same colors/radius are duplicated there as literal
  values rather than reading the CSS variables. Verified locally with real Stripe test keys set
  via `dotnet user-secrets` specifically to screenshot the themed payment form - the "Stripe not
  configured" honest-fallback state (see above) meant the Payment Element was never actually
  visible locally before this.
