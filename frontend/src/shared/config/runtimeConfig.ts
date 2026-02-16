type RuntimeEnv = {
  API_URL?: string;
  OTEL_ENDPOINT?: string;
};

declare global {
  interface Window {
    RUNTIME_ENV?: RuntimeEnv;
  }
}

const runtimeEnv: RuntimeEnv = typeof window !== 'undefined' ? window.RUNTIME_ENV ?? {} : {};

export const API_URL =
  runtimeEnv.API_URL ?? import.meta.env.VITE_API_URL ?? 'http://localhost:5156';

export const OTEL_ENDPOINT = runtimeEnv.OTEL_ENDPOINT ?? import.meta.env.VITE_OTEL_ENDPOINT ?? '';

export const runtimeConfig = {
  API_URL,
  OTEL_ENDPOINT,
} as const;
