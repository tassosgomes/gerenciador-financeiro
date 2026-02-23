import '@testing-library/jest-dom/vitest';

import { cleanup } from '@testing-library/react';
import { afterAll, afterEach, beforeAll } from 'vitest';

import { server } from './mocks/server';

// Polyfill for ResizeObserver (needed for Radix UI components)
global.ResizeObserver = class ResizeObserver {
  observe() {}
  unobserve() {}
  disconnect() {}
};

// Mock scrollIntoView
Element.prototype.scrollIntoView = () => {};

// Polyfill localStorage when test environment exposes an incomplete implementation
if (
  typeof window !== 'undefined' &&
  (typeof window.localStorage?.getItem !== 'function' ||
    typeof window.localStorage?.setItem !== 'function' ||
    typeof window.localStorage?.removeItem !== 'function' ||
    typeof window.localStorage?.clear !== 'function')
) {
  const storage = new Map<string, string>();

  Object.defineProperty(window, 'localStorage', {
    configurable: true,
    value: {
      getItem: (key: string) => storage.get(key) ?? null,
      setItem: (key: string, value: string) => {
        storage.set(key, String(value));
      },
      removeItem: (key: string) => {
        storage.delete(key);
      },
      clear: () => {
        storage.clear();
      },
    },
  });
}

// Align AbortController/AbortSignal between jsdom window and Node (undici)
if (typeof window !== 'undefined') {
  Object.defineProperty(window, 'AbortController', {
    configurable: true,
    value: globalThis.AbortController,
  });
  Object.defineProperty(window, 'AbortSignal', {
    configurable: true,
    value: globalThis.AbortSignal,
  });
}

beforeAll(() => server.listen());

afterEach(() => {
  server.resetHandlers();
  cleanup();
});

afterAll(() => server.close());
