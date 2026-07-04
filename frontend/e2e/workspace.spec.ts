import { expect, test } from '@playwright/test';

test.beforeEach(async ({ page }) => {
  await page.route('**/api/mortgage/base-rate', async (route) => {
    await route.fulfill({
      contentType: 'application/json',
      body: JSON.stringify({
        ratePercent: 4.25,
        effectiveDate: '2026-06-18',
        provenance: 'Live',
        source: 'Bank of England',
        note: 'Bank Rate is provided for orientation only.',
      }),
    });
  });

  await page.route('**/api/mortgage/estimate', async (route) => {
    await route.fulfill({
      contentType: 'application/json',
      body: JSON.stringify({
        loan: 372500,
        ltvPercent: 80.1,
        monthlyPayment: 2204.42,
        totalRepayment: 661326,
        totalInterest: 288826,
        stressTest: { ratePercent: 8.1, monthlyPayment: 2924.82 },
        assumptions: ['Repayment mortgage at 5.1% over 25 years.', 'Payments are monthly and on time.'],
        caveats: ['This is an estimate, not mortgage advice — speak to a qualified adviser before deciding.'],
      }),
    });
  });

  await page.route('**/api/copilot/ask', async (route) => {
    await route.fulfill({ status: 503, contentType: 'application/json', body: '{}' });
  });
});

test('leads with the conversation and opens the API-backed estimator', async ({ page }) => {
  await page.goto('/');

  // Conversation is the main surface.
  await expect(page.getByRole('heading', { name: /Compare areas and properties/i })).toBeVisible();
  await expect(page.getByText('not mortgage advice')).toBeVisible();

  // Estimator lives in the right rail.
  await page.getByRole('tab', { name: 'Estimator' }).click();
  await expect(page.getByText('£2,204.42')).toBeVisible();
  await expect(page.getByText('£372,500')).toBeVisible();
  await expect(page.getByText('+3% stress payment')).toBeVisible();
  await expect(page.getByText(/BoE base rate 4.25%/)).toBeVisible();
});

test('copilot composer degrades gracefully when unprovisioned', async ({ page }) => {
  await page.goto('/');

  await page.getByLabel('Ask HomeScout').fill('Compare SE10 vs CR0');
  await page.getByLabel('Ask HomeScout').press('Enter');

  await expect(page.getByText(/isn.t connected yet/i)).toBeVisible();
});

test('copilot answers populate the conversation and evidence rail', async ({ page }) => {
  await page.unroute('**/api/copilot/ask');
  await page.route('**/api/copilot/ask', async (route) => {
    await route.fulfill({
      contentType: 'application/json',
      body: JSON.stringify({
        text: 'For this Greenwich flat, the estimated monthly repayment is £2,204.42 and the loan-to-value is 80.1%.',
        toolCalls: [
          { name: 'estimate_mortgage', summary: 'Calculated repayment mortgage costs.' },
          { name: 'get_base_rate', summary: 'Checked Bank of England context.' },
        ],
        evidence: [
          {
            label: 'Monthly mortgage payment',
            value: '£2,204.42',
            kind: 'estimate',
            source: '/api/mortgage/estimate',
            provenance: 'Live',
          },
          {
            label: 'BoE base rate',
            value: '4.25%',
            kind: 'fact',
            source: 'Bank of England',
            provenance: 'Cache',
          },
        ],
        assumptions: ['Repayment mortgage at 5.1% over 25 years.'],
        caveats: ['This is an estimate, not mortgage advice — speak to a qualified adviser before deciding.'],
      }),
    });
  });

  await page.goto('/');

  await page.getByRole('button', { name: /Compare SE10 vs CR0/i }).click();

  await expect(page.getByText(/estimated monthly repayment is £2,204.42/i)).toBeVisible();
  await expect(page.getByText('estimate_mortgage')).toBeVisible();
  await expect(page.getByText('Evidence trail')).toBeVisible();
  await expect(page.getByText('Monthly mortgage payment')).toBeVisible();
  await expect(page.getByText('/api/mortgage/estimate')).toBeVisible();
  await expect(page.getByText('Cache')).toBeVisible();
});
