export const environment = {
  production: false,
  apiUrl: 'http://localhost:5131',
  // Test-mode publishable key - safe to ship client-side, but no real Stripe
  // account exists in this environment yet. Replace once one does; this is
  // read from here, never hardcoded in a component (guideline 06).
  stripePublishableKey: '',
};
