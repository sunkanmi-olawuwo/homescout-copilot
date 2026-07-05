import { afterEach, describe, expect, it } from 'vitest';
import { cleanup, render, screen } from '@testing-library/react';
import { ComparisonCard } from './ComparisonCard';
import type { ListingComparison } from '../types';

const buy: ListingComparison = {
  listing: { label: 'Stratford Way, YO32', mode: 'Buy', postcode: 'YO32', price: 500_000, priceQualifier: 'Guide', bedrooms: 3, floorArea: 1443, areaUnit: 'SquareFeet', tenure: 'Freehold', councilTaxBand: 'E' },
  pricePerSquareFoot: 347, pricePerSquareMetre: 3731, indicativeMonthlyCost: null,
  completenessPercent: 70, missingInformation: ['EPC rating', 'Full postcode'], notes: [],
};

describe('ComparisonCard', () => {
  afterEach(cleanup);

  it('leads with price per ft² and shows the sticker price for a Buy listing', () => {
    render(<ComparisonCard item={buy} />);
    expect(screen.getByText('£347')).toBeTruthy();
    expect(screen.getByText('/ft²')).toBeTruthy();
    expect(screen.getByText(/£500,000 guide/)).toBeTruthy();
    expect(screen.getByText('Buy')).toBeTruthy();
  });

  it('shows the completeness score and the "ask the agent" gaps', () => {
    render(<ComparisonCard item={buy} />);
    expect(screen.getByText('70%')).toBeTruthy();
    expect(screen.getByText('EPC rating')).toBeTruthy();
    expect(screen.getByText('Full postcode')).toBeTruthy();
  });

  it('leads with rent for a Rent listing', () => {
    const rent: ListingComparison = {
      listing: { label: 'Mordaunt Road, S2', mode: 'Rent', postcode: 'S2', monthlyRent: 925, furnishing: 'AtTenantChoice' },
      pricePerSquareFoot: null, pricePerSquareMetre: null, indicativeMonthlyCost: 925,
      completenessPercent: 60, missingInformation: [], notes: [],
    };
    render(<ComparisonCard item={rent} />);
    expect(screen.getAllByText('£925').length).toBeGreaterThan(0); // headline + indicative-monthly row
    expect(screen.getByText('/mo')).toBeTruthy();
    expect(screen.getByText("Tenant's choice")).toBeTruthy();
  });
});
