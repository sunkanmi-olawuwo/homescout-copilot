import type { FurnishingState, ListingComparison } from '../types';
import { currency0 } from '../lib/format';
import { MetricRow } from './MetricRow';

const FURNISHING: Record<FurnishingState, string> = {
  Furnished: 'Furnished',
  PartFurnished: 'Part furnished',
  Unfurnished: 'Unfurnished',
  AtTenantChoice: "Tenant's choice",
};

function area(floorArea?: number | null, unit?: string | null): string {
  if (floorArea == null) return '—';
  const suffix = unit === 'SquareMetres' ? 'm²' : 'ft²';
  return `${floorArea.toLocaleString('en-GB')} ${suffix}`;
}

// One listing's computed decision card: the headline metric the comparison adds (£/ft² for Buy, or
// rent), the key facts, a completeness bar, and the "ask the agent" gaps. Not a table row — a card.
export function ComparisonCard({ item }: { item: ListingComparison }) {
  const l = item.listing;
  const isBuy = l.mode === 'Buy';

  const headline = isBuy && item.pricePerSquareFoot != null
    ? { big: currency0(item.pricePerSquareFoot), unit: '/ft²', sub: `${currency0(l.price)}${l.priceQualifier === 'Guide' ? ' guide' : ''}` }
    : isBuy
      ? { big: currency0(l.price), unit: '', sub: 'add floor area for £/ft²' }
      : { big: currency0(l.monthlyRent), unit: '/mo', sub: `${currency0(item.indicativeMonthlyCost)} true monthly` };

  return (
    <article className="comparison-card">
      <header className="comp-card-head">
        <span className="comp-label">{l.label}</span>
        <span className={`comp-badge ${isBuy ? 'buy' : 'rent'}`}>{l.mode}</span>
      </header>

      <div className="comp-headline">
        <strong>{headline.big}</strong>
        <span className="comp-unit">{headline.unit}</span>
        <span className="comp-sub">{headline.sub}</span>
      </div>

      <dl className="metric-rows">
        <MetricRow label="Size" value={area(l.floorArea, l.areaUnit)} />
        <MetricRow label="Bedrooms" value={l.bedrooms == null ? '—' : String(l.bedrooms)} />
        {isBuy
          ? <MetricRow label="Tenure" value={l.tenure ?? '—'} />
          : <MetricRow label="Furnishing" value={l.furnishing ? FURNISHING[l.furnishing] : '—'} />}
        <MetricRow label="Council tax" value={l.councilTaxBand ? `Band ${l.councilTaxBand}` : '—'} />
        <MetricRow label="Indicative monthly" value={item.indicativeMonthlyCost == null ? 'add bills' : currency0(item.indicativeMonthlyCost)} />
      </dl>

      <div className="comp-complete">
        <span>Completeness <strong>{item.completenessPercent}%</strong></span>
        <div className="comp-bar" role="progressbar" aria-valuenow={item.completenessPercent} aria-valuemin={0} aria-valuemax={100}>
          <div className="comp-fill" style={{ width: `${item.completenessPercent}%` }} />
        </div>
      </div>

      {item.missingInformation.length > 0 ? (
        <div className="comp-ask">
          <span className="comp-ask-label">Ask the agent for:</span>
          <div className="comp-chips">
            {item.missingInformation.map((m) => (
              <span key={m} className="comp-chip">{m}</span>
            ))}
          </div>
        </div>
      ) : null}
    </article>
  );
}
