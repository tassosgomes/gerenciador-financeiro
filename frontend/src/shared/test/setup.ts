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

beforeAll(() => server.listen());

afterEach(() => {
  server.resetHandlers();
  cleanup();
});

afterAll(() => server.close());
