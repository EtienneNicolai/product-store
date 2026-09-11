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
