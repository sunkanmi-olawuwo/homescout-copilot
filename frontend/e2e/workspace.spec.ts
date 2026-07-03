import { expect, test } from '@playwright/test';

test('workspace shell renders the core regions', async ({ page }) => {
  await page.goto('/');

  await expect(page.getByRole('heading', { name: 'HomeScout' })).toBeVisible();
  await expect(
    page.getByRole('heading', { name: 'Property and area comparison' }),
  ).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Evidence' })).toBeVisible();
  await expect(
    page.getByRole('button', { name: 'Generate comparison' }),
  ).toBeVisible();
  await expect(page.getByText(/not mortgage advice/i)).toBeVisible();
});
