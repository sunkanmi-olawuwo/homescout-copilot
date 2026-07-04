import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import App from './App';

const estimateBody = {
  loan: 372500,
  ltvPercent: 80.1,
  monthlyPayment: 2204.42,
  totalRepayment: 661326,
  totalInterest: 288826,
  stressTest: { ratePercent: 8.1, monthlyPayment: 2924.82 },
  assumptions: ['Repayment mortgage at 5.1% over 25 years.', 'Payments are monthly and on time.'],
  caveats: ['This is an estimate, not mortgage advice — speak to a qualified adviser before deciding.'],
};

const baseRateBody = {
  ratePercent: 4.25,
  effectiveDate: '2026-06-18',
  provenance: 'Live',
  source: 'Bank of England',
  note: 'Bank Rate is provided for orientation only.',
};

function jsonResponse(body: unknown) {
  return { ok: true, status: 200, statusText: 'OK', json: () => Promise.resolve(body) } as Response;
}
function statusResponse(status: number) {
  return { ok: false, status, statusText: '', json: () => Promise.resolve({}) } as Response;
}

describe('App workspace', () => {
  beforeEach(() => {
    vi.stubGlobal(
      'fetch',
      vi.fn((input: RequestInfo | URL) => {
        const url = String(input);
        if (url.endsWith('/api/mortgage/base-rate')) return Promise.resolve(jsonResponse(baseRateBody));
        if (url.endsWith('/api/mortgage/estimate')) return Promise.resolve(jsonResponse(estimateBody));
        if (url.endsWith('/api/copilot/ask')) return Promise.resolve(statusResponse(503));
        return Promise.reject(new Error(`Unexpected request: ${url}`));
      }),
    );
  });

  afterEach(() => {
    cleanup();
    vi.unstubAllGlobals();
  });

  it('leads with the copilot conversation as the main surface', () => {
    render(<App />);

    expect(screen.getByLabelText('HomeScout Copilot')).toBeTruthy();
    expect(screen.getByRole('button', { name: /Greenwich vs Croydon/ })).toBeTruthy();
    expect(screen.getByRole('heading', { name: /Compare areas and properties/i })).toBeTruthy();
    // Estimator lives in the right rail, not the main surface.
    expect(screen.getByRole('tab', { name: 'Estimator' })).toBeTruthy();
    expect(screen.getByText(/not mortgage advice/i)).toBeTruthy();
  });

  it('shows the API-backed estimator in the right rail', async () => {
    render(<App />);

    fireEvent.click(screen.getByRole('tab', { name: 'Estimator' }));

    expect((await screen.findAllByText('£2,204.42')).length).toBeGreaterThan(0);
    expect(screen.getByText('£372,500')).toBeTruthy();
    expect(screen.getByText('80.1%')).toBeTruthy();
    expect(screen.getByText(/BoE base rate 4.25%/)).toBeTruthy();
    expect(screen.getAllByText('Live').length).toBeGreaterThan(0);
    // Total repayable is now surfaced (design parity), plus the +3% stress payment.
    expect(screen.getByText('Total repayable')).toBeTruthy();
    expect(screen.getByText('+3% stress payment')).toBeTruthy();
  });

  it('posts the typed mortgage estimate request (string enum) to the seam', async () => {
    const fetchMock = vi.mocked(fetch);
    render(<App />);

    await waitFor(() => {
      expect(
        fetchMock.mock.calls.some(
          ([url, init]) =>
            String(url) === '/api/mortgage/estimate' &&
            init?.method === 'POST' &&
            JSON.parse(String(init.body)).repaymentType === 'Repayment' &&
            JSON.parse(String(init.body)).propertyPrice === 465000,
        ),
      ).toBe(true);
    });

    fireEvent.click(screen.getByRole('tab', { name: 'Estimator' }));
    fireEvent.click(await screen.findByRole('button', { name: 'Interest-only' }));

    await waitFor(() => {
      expect(
        fetchMock.mock.calls.some(
          ([url, init]) =>
            String(url) === '/api/mortgage/estimate' &&
            init?.method === 'POST' &&
            JSON.parse(String(init.body)).repaymentType === 'InterestOnly',
        ),
      ).toBe(true);
    });
  });

  it('degrades gracefully when the copilot is not provisioned (503)', async () => {
    render(<App />);

    fireEvent.change(screen.getByLabelText('Ask HomeScout'), { target: { value: 'Compare SE10 vs CR0' } });
    fireEvent.submit(screen.getByLabelText('Ask HomeScout').closest('form') as HTMLFormElement);

    expect(await screen.findByText(/isn.t connected yet/i)).toBeTruthy();
  });

  it('supports the scoped light and dark themes', () => {
    const { container } = render(<App />);
    const app = container.querySelector('.homescout-app');

    expect(app?.getAttribute('data-theme')).toBe('light');
    fireEvent.click(screen.getByLabelText('Switch to dark theme'));
    expect(app?.getAttribute('data-theme')).toBe('dark');
  });
});
