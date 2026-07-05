// A labelled range slider with a formatted value readout, used by the estimator.
export function RangeField(props: {
  label: string;
  value: string;
  min: number;
  max: number;
  step: number;
  raw: number;
  onChange: (value: string) => void;
}) {
  return (
    <label className="range-field">
      <span>{props.label}<strong>{props.value}</strong></span>
      <input type="range" min={props.min} max={props.max} step={props.step} value={props.raw} onChange={(event) => props.onChange(event.target.value)} />
    </label>
  );
}
