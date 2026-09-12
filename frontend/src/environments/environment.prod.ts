export const environment = {
  production: true,
  // Render appends a random suffix when the exact requested service name is
  // already taken by someone else - "product-store-api" was, so this isn't
  // the plain deterministic URL originally assumed. Confirmed live (real
  // Kestrel/ASP.NET Core response, real seeded product data from Neon).
  apiUrl: 'https://product-store-api-g9ol.onrender.com',
  stripePublishableKey: 'pk_test_51UEq2k2QlVPWyXBP0agRmU1gFIUrBYFsnVE7xbVAdLoOmamG2Tka68ULnkSrxoZO5a4o35tFjls1pTiy3h0P74YP00W4qCCGFU',
};
