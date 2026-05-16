/**
 * Observability bootstrap. Called from main.ts BEFORE bootstrapApplication so
 * that the very first paint, the document-load timing, and any startup error
 * are captured.
 *
 * Two SDKs are wired up:
 *
 *  1. Sentry  — captures uncaught errors and unhandled promise rejections,
 *               attaches breadcrumbs (route changes, console logs, XHR calls),
 *               and sends performance transactions for route navigations.
 *               Free tier: 5k errors + 10k performance units per month.
 *
 *  2. OpenTelemetry (web SDK) — emits browser traces for the document load,
 *               every fetch/XHR call, and any custom spans. The traceparent
 *               header is attached to outgoing API calls, so backend spans
 *               (instrumented via the .NET OTel SDK) are linked to the same
 *               trace as the browser-side span that triggered them.
 *               Backend can be Grafana Cloud, Honeycomb, Tempo, or any OTLP
 *               receiver.
 *
 * Both SDKs become no-ops if their respective env vars are not set, so this
 * file works fine in local dev where you haven't signed up for anything yet.
 */

import * as Sentry from '@sentry/angular';

import { context, trace } from '@opentelemetry/api';
import { ZoneContextManager } from '@opentelemetry/context-zone';
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-http';
import { registerInstrumentations } from '@opentelemetry/instrumentation';
import { DocumentLoadInstrumentation } from '@opentelemetry/instrumentation-document-load';
import { FetchInstrumentation } from '@opentelemetry/instrumentation-fetch';
import { XMLHttpRequestInstrumentation } from '@opentelemetry/instrumentation-xml-http-request';
import { Resource } from '@opentelemetry/resources';
import { BatchSpanProcessor } from '@opentelemetry/sdk-trace-base';
import { WebTracerProvider } from '@opentelemetry/sdk-trace-web';
import { ATTR_SERVICE_NAME, ATTR_SERVICE_VERSION } from '@opentelemetry/semantic-conventions';

import { environment } from '../environments/environment';

export function initObservability(): void {
  // ---------- Sentry ----------
  if (environment.sentryDsn) {
    Sentry.init({
      dsn: environment.sentryDsn,
      environment: environment.production ? 'production' : 'development',
      release: environment.appVersion,
      integrations: [
        Sentry.browserTracingIntegration(),
        Sentry.replayIntegration({
          maskAllText: true,           // privacy by default
          blockAllMedia: true,
        }),
      ],
      tracesSampleRate: environment.production ? 0.1 : 1.0,
      replaysSessionSampleRate: 0,     // turn on selectively per route
      replaysOnErrorSampleRate: 1.0,   // always replay sessions that errored
      tracePropagationTargets: [environment.apiBaseUrl, /^\//],
      beforeSend(event) {
        // Drop events from localhost in production builds (e.g. dev hitting prod DSN).
        if (event.request?.url?.includes('localhost') && environment.production) return null;
        return event;
      },
    });
  }

  // ---------- OpenTelemetry browser tracer ----------
  if (environment.otelCollectorUrl) {
    const provider = new WebTracerProvider({
      resource: new Resource({
        [ATTR_SERVICE_NAME]: 'hoardly-web',
        [ATTR_SERVICE_VERSION]: environment.appVersion,
        'deployment.environment': environment.production ? 'production' : 'development',
      }),
    });

    provider.addSpanProcessor(
      new BatchSpanProcessor(
        new OTLPTraceExporter({
          url: environment.otelCollectorUrl,
          headers: environment.otelHeaders ?? {},
        }),
      ),
    );

    provider.register({ contextManager: new ZoneContextManager() });

    registerInstrumentations({
      instrumentations: [
        new DocumentLoadInstrumentation(),
        new FetchInstrumentation({
          propagateTraceHeaderCorsUrls: [/.+/g],
          clearTimingResources: true,
        }),
        new XMLHttpRequestInstrumentation({
          propagateTraceHeaderCorsUrls: [/.+/g],
        }),
      ],
    });

    // Marker span so we can verify wiring in the OTel backend.
    const tracer = trace.getTracer('hoardly-web');
    const span = tracer.startSpan('app.bootstrap');
    context.with(trace.setSpan(context.active(), span), () => span.end());
  }
}
