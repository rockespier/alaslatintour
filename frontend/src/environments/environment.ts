export const environment = {
  production: false,
  apiUrl: '',
  // Turnstile is enabled only for deployed hosts. This keeps the contact form
  // usable when developing on localhost, which is not a configured hostname.
  turnstileEnabled: false,
  turnstileSiteKey: '',
};
