export const environment = {
  production: true,
  apiBaseUrl: '/api/v1',
  appVersion: '0.1.0',

  // ---------- Observability ----------
  // In production these are typically baked at build time via CI:
  //   ng build --configuration production \
  //     --define "process.env.SENTRY_DSN=$SENTRY_DSN" ...
  // or read from runtime config injected by nginx. For now they're blank
  // and you can patch them in your deploy pipeline.
  sentryDsn: '',
  otelCollectorUrl: '',
  otelHeaders: undefined as Record<string, string> | undefined,
};
