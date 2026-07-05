// A label/value row in the estimator's metric list (rendered inside a <dl>).
export function MetricRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="metric-row">
      <dt>{label}</dt>
      <dd>{value}</dd>
    </div>
  );
}
