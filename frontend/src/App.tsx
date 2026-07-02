import { useEffect, useState } from 'react';
import './App.css';

interface ApiStatus {
  product: string;
  frontend: string;
  architecture: string;
  agentPlatform: string;
}

const savedSearches = [
  { name: 'Greenwich vs Croydon', meta: 'Commute, schools, budget' },
  { name: 'Reading family homes', meta: 'Parks, rail, price context' },
  { name: 'Canary Wharf flats', meta: 'Service charge, commute' },
];

const evidenceItems = [
  { label: 'Crime', value: 'Awaiting Police.uk tool' },
  { label: 'Schools', value: 'Awaiting schools data source' },
  { label: 'Amenities', value: 'Awaiting OpenStreetMap tool' },
  { label: 'Costs', value: 'Estimator planned first' },
];

function App() {
  const [apiStatus, setApiStatus] = useState<ApiStatus | null>(null);
  const [prompt, setPrompt] = useState(
    'Compare a 2-bed flat in SE10 with one in CR0. I care about commute, schools, nearby parks, crime context, and monthly ownership costs.'
  );

  useEffect(() => {
    void fetch('/api/status')
      .then((response) => response.json())
      .then((data: ApiStatus) => setApiStatus(data))
      .catch(() => {
        setApiStatus({
          product: 'HomeScout Copilot',
          frontend: 'React',
          architecture: 'API-first',
          agentPlatform: 'Microsoft Foundry Agent Service planned',
        });
      });
  }, []);

  return (
    <main className="workspace-shell">
      <aside className="sidebar" aria-label="Saved searches">
        <div className="brand-block">
          <span className="brand-mark" aria-hidden="true">H</span>
          <div>
            <h1>HomeScout</h1>
            <p>Buyer workspace</p>
          </div>
        </div>

        <nav className="search-list" aria-label="Saved comparisons">
          {savedSearches.map((search) => (
            <button className="search-item" type="button" key={search.name}>
              <span>{search.name}</span>
              <small>{search.meta}</small>
            </button>
          ))}
        </nav>
      </aside>

      <section className="comparison-panel" aria-label="Comparison workspace">
        <header className="workspace-header">
          <div>
            <p className="eyebrow">API-first React client</p>
            <h2>Property and area comparison</h2>
          </div>
          <div className="status-pill" title="Backend connection">
            {apiStatus?.frontend ?? 'React'} / {apiStatus?.architecture ?? 'API-first'}
          </div>
        </header>

        <div className="comparison-grid">
          <section className="input-panel" aria-label="Comparison request">
            <label htmlFor="comparison-prompt">Comparison request</label>
            <textarea
              id="comparison-prompt"
              value={prompt}
              onChange={(event) => setPrompt(event.target.value)}
            />
            <div className="action-row">
              <button type="button" className="primary-action">Generate comparison</button>
              <button type="button" className="secondary-action">Attach listing</button>
            </div>
          </section>

          <section className="report-panel" aria-label="Draft report">
            <div className="report-header">
              <span>Draft report</span>
              <small>{apiStatus?.agentPlatform ?? 'Foundry Agent Service planned'}</small>
            </div>
            <div className="report-body">
              <p>
                HomeScout will route this request through the API, collect structured evidence,
                and stream a comparison report back to this workspace.
              </p>
              <p>
                The first implementation target is a deterministic ownership-cost estimator,
                followed by public-data tools for amenities, crime context, schools, and price history.
              </p>
            </div>
          </section>
        </div>
      </section>

      <aside className="evidence-panel" aria-label="Evidence panel">
        <h2>Evidence</h2>
        <div className="evidence-list">
          {evidenceItems.map((item) => (
            <div className="evidence-item" key={item.label}>
              <span>{item.label}</span>
              <strong>{item.value}</strong>
            </div>
          ))}
        </div>
        <div className="boundary-note">
          Decision support only. Estimates and public-data summaries are not mortgage advice.
        </div>
      </aside>
    </main>
  );
}

export default App;
