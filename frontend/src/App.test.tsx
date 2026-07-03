import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { cleanup, render, screen } from '@testing-library/react';
import App from './App';

describe('App workspace shell', () => {
  beforeEach(() => {
    // App calls /api/status on mount; stub fetch so the component uses its fallback
    // and the test has no network dependency.
    vi.stubGlobal(
      'fetch',
      vi.fn(() => Promise.reject(new Error('no network in test'))),
    );
  });

  afterEach(() => {
    cleanup();
    vi.unstubAllGlobals();
  });

  it('renders the core workspace regions', () => {
    render(<App />);

    expect(screen.getByRole('heading', { name: 'HomeScout' })).toBeTruthy();
    expect(
      screen.getByRole('heading', { name: 'Property and area comparison' }),
    ).toBeTruthy();
    expect(screen.getByRole('heading', { name: 'Evidence' })).toBeTruthy();
    expect(screen.getByText(/not mortgage advice/i)).toBeTruthy();
  });
});
