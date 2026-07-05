import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { ComparisonView } from './ComparisonView';
import * as client from '../api/client';
import type { ComparisonResult, ListingExtractionResult } from '../types';

vi.mock('../api/client', () => ({ extractListing: vi.fn(), compareListings: vi.fn() }));

const draft: ListingExtractionResult = {
  draft: { label: 'A place, AB1', mode: 'Rent', postcode: 'AB1', monthlyRent: 900 },
  fields: [{ field: 'MonthlyRent', source: 'Text', confidence: 'High' }],
  notes: ['No floor area on the listing — add it for price per ft².'],
};

const result: ComparisonResult = {
  listings: [
    { listing: { label: 'A place, AB1', mode: 'Rent', postcode: 'AB1', monthlyRent: 900 }, pricePerSquareFoot: null, pricePerSquareMetre: null, indicativeMonthlyCost: 900, completenessPercent: 55, missingInformation: ['Floor area'], notes: [] },
    { listing: { label: 'B place, CD2', mode: 'Rent', postcode: 'CD2', monthlyRent: 800 }, pricePerSquareFoot: null, pricePerSquareMetre: null, indicativeMonthlyCost: 800, completenessPercent: 65, missingInformation: [], notes: [] },
  ],
  highlights: ['Lowest indicative monthly cost: B place, CD2 at £800/month.'],
  assumptions: ['Indicative monthly cost is a running-cost figure.'],
  caveats: ['This compares the facts you entered — not property, mortgage, or tenancy advice.'],
};

async function addListing() {
  fireEvent.click(screen.getByRole('button', { name: '+ Add listing' }));
  const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement;
  fireEvent.change(fileInput, { target: { files: [new File(['%PDF'], 'listing.pdf', { type: 'application/pdf' })] } });
  fireEvent.click(screen.getByRole('button', { name: 'Extract facts' }));
  fireEvent.click(await screen.findByRole('button', { name: 'Add to comparison' }));
}

describe('ComparisonView', () => {
  beforeEach(() => {
    vi.mocked(client.extractListing).mockResolvedValue(draft);
    vi.mocked(client.compareListings).mockResolvedValue(result);
  });
  afterEach(() => { cleanup(); vi.clearAllMocks(); });

  it('shows an empty state prompting for at least two listings', () => {
    render(<ComparisonView />);
    expect(screen.getByText(/Add at least two listings to compare/)).toBeTruthy();
  });

  it('opens the capture flow and confirms an extracted draft', async () => {
    render(<ComparisonView />);
    fireEvent.click(screen.getByRole('button', { name: '+ Add listing' }));
    expect(screen.getByText('Add a listing')).toBeTruthy();

    const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement;
    fireEvent.change(fileInput, { target: { files: [new File(['%PDF'], 'listing.pdf', { type: 'application/pdf' })] } });
    fireEvent.click(screen.getByRole('button', { name: 'Extract facts' }));

    // Confirm step shows the extracted draft with its provenance/confidence badge.
    expect(await screen.findByText('Confirm the facts')).toBeTruthy();
    expect((screen.getByDisplayValue('A place, AB1'))).toBeTruthy();
    expect(screen.getByText('High')).toBeTruthy();
  });

  it('builds the side-by-side once two listings are added', async () => {
    render(<ComparisonView />);
    await addListing();
    await addListing();

    await waitFor(() => expect(client.compareListings).toHaveBeenCalled());
    expect(vi.mocked(client.compareListings).mock.calls[0][0]).toHaveLength(2);

    // The decision cards + a descriptive highlight render.
    expect(await screen.findByText(/Lowest indicative monthly cost/)).toBeTruthy();
    const cards = await screen.findAllByText(/place,/);
    expect(cards.length).toBeGreaterThanOrEqual(2);
    expect(screen.getByText(/not property, mortgage, or tenancy advice/)).toBeTruthy();
  });

  it('removes a listing via its pill', async () => {
    render(<ComparisonView />);
    await addListing();
    const pills = screen.getByRole('list', { name: '' }) ?? document.querySelector('.comparison-pills');
    void pills;
    fireEvent.click(within(document.querySelector('.comparison-pills') as HTMLElement).getByRole('button', { name: /Remove/ }));
    expect(document.querySelector('.comparison-pills')).toBeNull();
  });
});
