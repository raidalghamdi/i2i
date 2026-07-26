export const environment = {
  production: false,
  devUser: 'devuser',
  // Selectable users for the dev-user switcher dropdown. Each value is a sam-account name
  // sent as the X-Dev-User header. Edit freely — anything you set via
  // localStorage['devUser'] directly still works even if it's not listed here.
  devUsers: ['devuser', 'admin1', 'supervisor1', 'evaluator1', 'submitter3', 'judge3', 'expert1', 'mentor1', 'facilitator1'],
};
