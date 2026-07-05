// GBP / percentage formatting helpers. "Missing" mirrors the backend's missing-figure semantics.

const gbp0 = new Intl.NumberFormat('en-GB', { style: 'currency', currency: 'GBP', maximumFractionDigits: 0 });
const gbp2 = new Intl.NumberFormat('en-GB', { style: 'currency', currency: 'GBP', minimumFractionDigits: 2, maximumFractionDigits: 2 });

export const pct1 = new Intl.NumberFormat('en-GB', { maximumFractionDigits: 1 });

export function currency0(value: number | null | undefined) {
  return value === null || value === undefined || Number.isNaN(value) ? 'Missing' : gbp0.format(value);
}

export function currency2(value: number | null | undefined) {
  return value === null || value === undefined || Number.isNaN(value) ? 'Missing' : gbp2.format(value);
}
