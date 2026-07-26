export const environment = {
  production: true,
  devUser: '',
  // Dev-only impersonation list (see environment.development.ts). Empty in production
  // because the dev-user switcher is hidden and the X-Dev-User header is a no-op there.
  devUsers: [] as string[],
};
