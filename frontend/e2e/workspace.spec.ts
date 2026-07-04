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
        stressTest: {
          ratePercent: 8.1,
          monthlyPayment: 2924.82,
        },
        assumptions: [
          'Repayment mortgage at 5.1% over 25 years.',
          'Payments are monthly and on time.',
        ],
        caveats: [
          'This is an estimate, not mortgage advice - speak to a qualified mortgage adviser.',
        ],
      }),
    });
  });
});

test('mortgage estimator renders with API-backed figures', async ({ page }) => {
  await page.goto('/');

  await expect(
    page.getByRole('heading', { name: 'Mortgage cost estimator' }),
  ).toBeVisible();
  await expect(
    page.getByLabel('Mortgage estimate result').getByText('£2,204.42'),
  ).toBeVisible();
  await expect(page.getByText('£2,980.42 / mo')).toBeVisible();
  await expect(page.getByText('BoE base rate 4.25%')).toBeVisible();
  await expect(page.getByText('not mortgage advice')).toBeVisible();
});
