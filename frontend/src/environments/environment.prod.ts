export const environment = {
  production: true,
  // Render URLs are deterministic from the service name chosen at creation
  // time (https://<service-name>.onrender.com) - this assumes the backend
  // Web Service is named "product-store-api". If it's named differently,
  // update this to match.
  apiUrl: 'https://product-store-api.onrender.com',
  stripePublishableKey: '',
};
