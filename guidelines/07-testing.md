# Guideline 07 - Testing

## Goal
Real automated coverage, not a token test file.

## Owns
- `backend/Store.Api.Tests/` - xUnit tests for `ProductsController`, `CartController`,
  `CheckoutController`
- Angular component tests for the checkout flow specifically (the highest-risk area)

## Requirements
- Mock the Stripe client (`Moq` against `Stripe.net`'s interfaces) - tests must never make a
  real call to Stripe's API, even in test mode
- Use an EF Core in-memory database provider (or SQLite in-memory) for controller tests, not
  the real SQL Server LocalDB instance - tests should not depend on a database server being up
- Cover the webhook handler's signature verification failure path explicitly (an invalid or
  missing signature must be rejected, not silently accepted) - this is the single most
  important thing to test in the entire project, since it is the one place fake requests could
  otherwise mark an unpaid order as paid
- At minimum, one Angular test confirming the checkout component correctly calls
  `create-payment-intent` and handles both success and failure responses

## Depends on
Sessions 1-4 for the backend tests, Session 5 for the frontend tests. Can start once the
relevant controllers/components exist, does not need to wait for every other session to
finish entirely.

## Completion notes
- **Stripe.net 52.4.2 has no `IPaymentIntentService` interface** (older versions did) and no
  `EventUtility.GenerateTestHeaderStringForPayload` test helper either. Two real consequences:
  - `CheckoutController` was refactored to depend on a small interface this project owns,
    `IPaymentIntentGateway` (`Services/IPaymentIntentGateway.cs`), instead of constructing
    `new PaymentIntentService(new StripeClient(secretKey))` inline. The secret key is still read
    per-request from configuration (Key Trap 5), just passed as an argument rather than baked
    into a client at construction time. This is what makes the controller mockable with Moq at
    all - decoupling from Stripe.net's own class shape turned out to be a feature, not just a
    workaround.
  - Test payload signing implements Stripe's public webhook signature scheme directly
    (`HMACSHA256("{timestamp}.{payload}")`, see `WebhookTests.SignPayload`) rather than calling
    a helper that doesn't exist in this SDK version. Verification itself still happens for real
    inside the controller via `EventUtility.ConstructEvent` - only the *test-side signing* is
    hand-rolled, and it's the same algorithm Stripe's own servers use, not a fake.
  - `CreatePaymentIntentResponse` gained `OrderId` in Session 5 - the mock's `PaymentIntent`
    return values in these tests need at minimum `Id`/`ClientSecret` set for the controller to
    build a valid response from them.
- **Sanity-checked that the webhook tests actually catch a regression**, not just pass
  vacuously: temporarily replaced the real `EventUtility.ConstructEvent` signature check with a
  plain deserialize (skipping verification entirely), reran the suite, confirmed all three
  signature-rejection tests failed as expected, then restored the real code and confirmed all 25
  tests passed again. This is the single most important test in the project per this guideline,
  worth the extra step of proving it isn't a false positive.
- Integration tests use `WebApplicationFactory<Program>` against a real, isolated in-memory
  SQLite database (an open `SqliteConnection` kept alive for the factory's lifetime - closing it
  destroys an in-memory SQLite DB) rather than EF's separate `InMemory` provider, for higher
  fidelity to what SQLite actually enforces. `Program.cs` needed `public partial class Program
  {}` added at the bottom - top-level statements generate an internal one by default, which
  `WebApplicationFactory` can't reference from the test project without it.
- `ProductsControllerTests`/`CartControllerTests` share one `StoreApiFactory` per class via
  `IClassFixture` (cheaper - one host/DB per class, not per test); test isolation comes from each
  test using its own session cookie via the `TestClient` helper, not from a fresh database every
  time. `CheckoutControllerTests`/`WebhookTests` build a fresh factory per test instead, since
  those tests configure `Mock<IPaymentIntentGateway>` differently per test and a shared mock's
  setups would leak between them.
- Frontend: `checkout.spec.ts` covers the guideline's explicit minimum (calls
  `create-payment-intent` with the entered email, handles both success and failure). `loadStripe`
  is a free function import, not an injectable, so it's mocked via Vitest's `vi.mock` on the
  `@stripe/stripe-js` module rather than through Angular DI. `Checkout`'s `email`/`stage`/
  `submittingEmail`/`error` signals were widened from `protected` to public specifically so the
  spec can drive them directly - full template-driven testing through the DOM would mean also
  contending with the mocked Stripe Elements mount lifecycle for no real benefit over what this
  guideline actually asks for.
