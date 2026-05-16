export const environment = {
  production: false,
  apiBaseUrl: 'http://localhost:5000/api/v1',
  appVersion: '0.1.0',

  // ---------- Observability (optional; leave blank to disable) ----------
  // Sentry DSN. Get one at https://sentry.io (free tier: 5k errors/month).
  sentryDsn: '',

  // OpenTelemetry OTLP HTTP endpoint for browser traces. Examples:
  //   Grafana Cloud:        https://otlp-gateway-<region>.grafana.net/otlp/v1/traces
  //   Local OTel Collector: http://localhost:4318/v1/traces
  otelCollectorUrl: '',
  otelHeaders: undefined as Record<string, string> | undefined,
};
