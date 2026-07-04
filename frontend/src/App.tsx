import type { ReactNode } from 'react';
import { useEffect, useState } from 'react';
import './App.css';

type Theme = 'light' | 'dark';
type RepaymentType = 'Repayment' | 'InterestOnly';
type Provenance = 'Live' | 'Cache' | 'Fallback';
type FigureKind = 'fact' | 'estimate' | 'assumption' | 'missing';
type MainTab = 'conversation' | 'comparison';
type RightTab = 'evidence' | 'estimator';

interface MortgageEstimateRequest {
  propertyPrice: number;
  deposit: number;
  annualInterestRatePercent: number;
  termYears: number;
  repaymentType: RepaymentType;
}

interface MortgageStressTest {
  ratePercent: number;
  monthlyPayment: number;
}

interface MortgageEstimateResult {
  loan: number;
  ltvPercent: number;
  monthlyPayment: number;
  totalRepayment: number | null;
  totalInterest: number;
  stressTest: MortgageStressTest;
  assumptions: string[];
  caveats: string[];
}

interface BaseRate {
  ratePercent: number;
  effectiveDate: string;
  provenance: Provenance;
  source: string;
  note: string;
}

interface CopilotToolCall {
  name: string;
  summary: string;
}

interface EvidenceItem {
  label: string;
  value: string;
  kind: FigureKind;
  source: string;
  provenance: Provenance | null;
}

interface CopilotAnswer {
  text: string;
  toolCalls: CopilotToolCall[];
  evidence: EvidenceItem[];
  assumptions: string[];
  caveats: string[];
}

const savedComparisons = [
  { name: 'Greenwich vs Croydon', meta: 'Commute · schools · monthly cost', age: 'edited just now · 2 areas', active: true },
  { name: 'Reading family homes', meta: 'Parks · rail · price context', age: '3 days ago · 3 areas', active: false },
  { name: 'Canary Wharf flats', meta: 'Service charge · commute', age: '1 week ago · 2 areas', active: false },
];

const startPrompts = [
  { title: 'Compare SE10 vs CR0', body: '2-bed flat, on commute, schools, parks, crime context & monthly cost' },
  { title: 'What would this cost me monthly?', body: 'Ownership cost on your own rate, with a +3% stress test', opensEstimator: true },
  { title: 'What should I ask at the viewing?', body: 'Questions worth asking for each area, grounded in the data' },
  { title: 'Upload a listing, EPC or survey', body: 'Extract facts to feed the comparison', upload: true },
];

const gbp0 = new Intl.NumberFormat('en-GB', { style: 'currency', currency: 'GBP', maximumFractionDigits: 0 });
const gbp2 = new Intl.NumberFormat('en-GB', { style: 'currency', currency: 'GBP', minimumFractionDigits: 2, maximumFractionDigits: 2 });
const pct1 = new Intl.NumberFormat('en-GB', { maximumFractionDigits: 1 });

const initialRequest: MortgageEstimateRequest = {
  propertyPrice: 465_000,
  deposit: 92_500,
  annualInterestRatePercent: 5.1,
  termYears: 25,
  repaymentType: 'Repayment',
};

function currency0(value: number | null | undefined) {
  return value === null || value === undefined || Number.isNaN(value) ? 'Missing' : gbp0.format(value);
}
function currency2(value: number | null | undefined) {
  return value === null || value === undefined || Number.isNaN(value) ? 'Missing' : gbp2.format(value);
}

function isSafeLinkUrl(url: string) {
  const trimmed = url.trim();
  if (trimmed.startsWith('/')) return true;
  try {
    const parsed = new URL(trimmed);
    return ['http:', 'https:', 'mailto:'].includes(parsed.protocol);
  } catch {
    return false;
  }
}

function pushText(nodes: ReactNode[], text: string, key: string) {
  if (text) nodes.push(<span key={key}>{text}</span>);
}

function findNextMarkdownToken(source: string, from: number) {
  const candidates = [
    source.indexOf('`', from),
    source.indexOf('**', from),
    source.indexOf('[', from),
    source.indexOf('*', from),
  ].filter((index) => index >= 0);

  return candidates.length ? Math.min(...candidates) : -1;
}

function renderInlineMarkdown(source: string, keyPrefix: string): ReactNode[] {
  const nodes: ReactNode[] = [];
  let cursor = 0;
  let key = 0;

  while (cursor < source.length) {
    const next = findNextMarkdownToken(source, cursor);
    if (next < 0) {
      pushText(nodes, source.slice(cursor), `${keyPrefix}-text-${key++}`);
      break;
    }

    pushText(nodes, source.slice(cursor, next), `${keyPrefix}-text-${key++}`);

    if (source.startsWith('`', next)) {
      const end = source.indexOf('`', next + 1);
      if (end > next) {
        nodes.push(<code key={`${keyPrefix}-code-${key++}`}>{source.slice(next + 1, end)}</code>);
        cursor = end + 1;
        continue;
      }
    }

    if (source.startsWith('**', next)) {
      const end = source.indexOf('**', next + 2);
      if (end > next) {
        nodes.push(<strong key={`${keyPrefix}-strong-${key++}`}>{source.slice(next + 2, end)}</strong>);
        cursor = end + 2;
        continue;
      }
    }

    if (source.startsWith('[', next)) {
      const labelEnd = source.indexOf(']', next + 1);
      const urlStart = labelEnd >= 0 ? source.indexOf('(', labelEnd + 1) : -1;
      const urlEnd = urlStart >= 0 ? source.indexOf(')', urlStart + 1) : -1;
      if (labelEnd > next && urlStart === labelEnd + 1 && urlEnd > urlStart) {
        const label = source.slice(next + 1, labelEnd);
        const url = source.slice(urlStart + 1, urlEnd);
        if (isSafeLinkUrl(url)) {
          nodes.push(
            <a key={`${keyPrefix}-link-${key++}`} href={url.trim()} target="_blank" rel="noreferrer noopener">
              {label}
            </a>,
          );
        } else {
          pushText(nodes, label, `${keyPrefix}-unsafe-link-${key++}`);
        }
        cursor = urlEnd + 1;
        continue;
      }
    }

    if (source.startsWith('*', next) && !source.startsWith('**', next)) {
      const end = source.indexOf('*', next + 1);
      if (end > next) {
        nodes.push(<em key={`${keyPrefix}-em-${key++}`}>{source.slice(next + 1, end)}</em>);
        cursor = end + 1;
        continue;
      }
    }

    pushText(nodes, source[next], `${keyPrefix}-literal-${key++}`);
    cursor = next + 1;
  }

  return nodes;
}

function renderMarkdownBlocks(source: string) {
  const lines = source.replace(/\r\n/g, '\n').trim().split('\n');
  const blocks: ReactNode[] = [];
  let index = 0;

  while (index < lines.length) {
    const line = lines[index].trim();
    if (!line) {
      index += 1;
      continue;
    }

    const heading = /^(#{1,3})\s+(.+)$/.exec(line);
    if (heading) {
      blocks.push(<h2 key={`heading-${index}`}>{renderInlineMarkdown(heading[2], `heading-${index}`)}</h2>);
      index += 1;
      continue;
    }

    const bulletItems: string[] = [];
    while (index < lines.length) {
      const bullet = /^\s*[-*]\s+(.+)$/.exec(lines[index]);
      if (!bullet) break;
      bulletItems.push(bullet[1]);
      index += 1;
    }
    if (bulletItems.length) {
      blocks.push(
        <ul key={`ul-${index}`}>
          {bulletItems.map((item, itemIndex) => (
            <li key={`${item}-${itemIndex}`}>{renderInlineMarkdown(item, `ul-${index}-${itemIndex}`)}</li>
          ))}
        </ul>,
      );
      continue;
    }

    const numberedItems: string[] = [];
    while (index < lines.length) {
      const item = /^\s*\d+\.\s+(.+)$/.exec(lines[index]);
      if (!item) break;
      numberedItems.push(item[1]);
      index += 1;
    }
    if (numberedItems.length) {
      blocks.push(
        <ol key={`ol-${index}`}>
          {numberedItems.map((item, itemIndex) => (
            <li key={`${item}-${itemIndex}`}>{renderInlineMarkdown(item, `ol-${index}-${itemIndex}`)}</li>
          ))}
        </ol>,
      );
      continue;
    }

    const paragraphLines: string[] = [];
    while (index < lines.length) {
      const nextLine = lines[index].trim();
      if (!nextLine || /^(#{1,3})\s+/.test(nextLine) || /^\s*[-*]\s+/.test(lines[index]) || /^\s*\d+\.\s+/.test(lines[index])) {
        break;
      }
      paragraphLines.push(nextLine);
      index += 1;
    }
    blocks.push(
      <p key={`p-${index}`}>
        {renderInlineMarkdown(paragraphLines.join(' '), `p-${index}`)}
      </p>,
    );
  }

  return blocks;
}

async function readJson<T>(url: string, options?: RequestInit, signal?: AbortSignal): Promise<T> {
  const response = await fetch(url, { ...options, signal });
  if (!response.ok) {
    throw new Error(`${response.status} ${response.statusText}`);
  }
  return (await response.json()) as T;
}

function useViewport() {
  const [width, setWidth] = useState(() => (typeof window === 'undefined' ? 1280 : window.innerWidth));
  useEffect(() => {
    const onResize = () => setWidth(window.innerWidth);
    window.addEventListener('resize', onResize);
    return () => window.removeEventListener('resize', onResize);
  }, []);
  return width;
}

function App() {
  const [theme, setTheme] = useState<Theme>('light');
  const [mainTab, setMainTab] = useState<MainTab>('conversation');
  const [rightTab, setRightTab] = useState<RightTab>('evidence');
  const [navOpen, setNavOpen] = useState(false);

  const [request, setRequest] = useState<MortgageEstimateRequest>(initialRequest);
  const [estimate, setEstimate] = useState<MortgageEstimateResult | null>(null);
  const [estimateError, setEstimateError] = useState<string | null>(null);
  const [baseRate, setBaseRate] = useState<BaseRate | null>(null);
  const [copilotNotice, setCopilotNotice] = useState<string | null>(null);
  const [copilotAnswer, setCopilotAnswer] = useState<CopilotAnswer | null>(null);
  const [copilotQuestion, setCopilotQuestion] = useState<string | null>(null);
  const [isAskingCopilot, setIsAskingCopilot] = useState(false);

  const viewport = useViewport();
  const isMobile = viewport < 760;
  const conversationActive = Boolean(copilotQuestion || copilotNotice || copilotAnswer || isAskingCopilot);

  useEffect(() => {
    const controller = new AbortController();
    void readJson<BaseRate>('/api/mortgage/base-rate', undefined, controller.signal)
      .then((data) => setBaseRate(data))
      .catch(() => {
        if (!controller.signal.aborted) setBaseRate(null);
      });
    return () => controller.abort();
  }, []);

  useEffect(() => {
    const controller = new AbortController();
    void readJson<MortgageEstimateResult>(
      '/api/mortgage/estimate',
      { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(request) },
      controller.signal,
    )
      .then((data) => {
        setEstimate(data);
        setEstimateError(null);
      })
      .catch((error: Error) => {
        if (controller.signal.aborted) return;
        setEstimate(null);
        setEstimateError(error.message);
      });
    return () => controller.abort();
  }, [request]);

  const depositPercent = request.propertyPrice > 0 ? Math.round((request.deposit / request.propertyPrice) * 100) : 0;

  const askCopilot = async (message: string) => {
    const trimmed = message.trim();
    if (!trimmed || isAskingCopilot) return;
    setCopilotQuestion(trimmed);
    setCopilotNotice(null);
    setCopilotAnswer(null);
    setRightTab('evidence');
    setIsAskingCopilot(true);
    try {
      const response = await fetch('/api/copilot/ask', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ message: trimmed }),
      });
      if (response.status === 503) {
        setCopilotNotice('HomeScout’s copilot isn’t connected yet — the reasoning agent is still being provisioned. Meanwhile, the Estimator is live: open it from the panel.');
        return;
      }
      if (!response.ok) {
        setCopilotNotice(`Copilot request failed (${response.status}). The Estimator remains available.`);
        return;
      }
      const answer = (await response.json()) as CopilotAnswer;
      setCopilotAnswer(answer);
    } catch {
      setCopilotNotice('Could not reach the HomeScout API. The Estimator uses the same API and will show its own status.');
    } finally {
      setIsAskingCopilot(false);
    }
  };

  return (
    <div className="homescout-app" data-theme={theme} data-viewport={isMobile ? 'mobile' : 'wide'}>
      <header className="top-bar">
        {isMobile ? (
          <button
            className="icon-button nav-toggle"
            type="button"
            aria-label={navOpen ? 'Close navigation' : 'Open navigation'}
            aria-expanded={navOpen}
            onClick={() => setNavOpen((open) => !open)}
          >
            ☰
          </button>
        ) : null}
        <div className="brand-lockup" aria-label="HomeScout Copilot">
          <span className="brand-mark" aria-hidden="true">H</span>
          <div>
            <strong>HomeScout <span>Copilot</span></strong>
            <small>Greenwich vs Croydon · draft</small>
          </div>
        </div>
        <span className="decision-pill">Decision support, not advice</span>
        <div className="top-actions">
          <button className="toolbar-button" type="button">Search<kbd>⌘K</kbd></button>
          <button
            className="icon-button"
            type="button"
            aria-label={`Switch to ${theme === 'dark' ? 'light' : 'dark'} theme`}
            title={`Switch to ${theme === 'dark' ? 'light' : 'dark'} theme`}
            onClick={() => setTheme((current) => (current === 'dark' ? 'light' : 'dark'))}
          >
            {theme === 'dark' ? '☀' : '◐'}
          </button>
          <button className="avatar-button" type="button" aria-label="Account">AO</button>
        </div>
      </header>

      <div className="workspace-grid">
        <aside
          className={`left-rail${navOpen ? ' open' : ''}`}
          aria-label="Saved comparisons and tools"
          aria-hidden={isMobile && !navOpen}
        >
          <button className="new-comparison" type="button">+ New comparison</button>
          <label className="rail-search">
            <span aria-hidden="true">⌕</span>
            <input type="search" placeholder="Filter saved searches" aria-label="Filter saved searches" />
          </label>
          <section aria-labelledby="saved-comparisons-heading">
            <h2 id="saved-comparisons-heading">Saved comparisons</h2>
            <div className="saved-list">
              {savedComparisons.map((comparison) => (
                <button
                  type="button"
                  className={`saved-item${comparison.active ? ' active' : ''}`}
                  key={comparison.name}
                >
                  <strong>{comparison.name}</strong>
                  <span>{comparison.meta}</span>
                  <small>{comparison.age}</small>
                </button>
              ))}
            </div>
          </section>
          <nav className="rail-footer" aria-label="Workspace">
            <button type="button">Case file<span className="badge">2</span></button>
            <button type="button">Preferences<span className="badge">5</span></button>
            <button type="button">Settings</button>
          </nav>
        </aside>
        {isMobile && navOpen ? (
          <button className="rail-scrim" type="button" aria-label="Close navigation" onClick={() => setNavOpen(false)} />
        ) : null}

        <main className="main-workspace" aria-label="HomeScout workspace">
          <div className="workspace-tabs" role="tablist" aria-label="Workspace views">
            <button type="button" role="tab" aria-selected={mainTab === 'conversation'} onClick={() => setMainTab('conversation')}>
              Conversation
            </button>
            <button
              type="button"
              role="tab"
              aria-selected={mainTab === 'comparison'}
              onClick={() => {
                setMainTab('comparison');
                if (isMobile) setRightTab('estimator');
              }}
            >
              {isMobile ? 'Estimator' : 'Comparison'}
            </button>
          </div>

          {mainTab === 'conversation' || !isMobile ? (
            <section className={`conversation${conversationActive ? ' active' : ''}`} aria-label="HomeScout copilot">
              <span className="status-pill"><i aria-hidden="true" />Copilot ready · public-data tools connected</span>
              <h1>Compare areas and properties, with the evidence shown.</h1>
              <p className="conversation-lead">
                Ask HomeScout to compare two or three postcodes or listings. It reasons over public data, your
                uploads and saved preferences — and every figure keeps its source, freshness and assumptions.
                This is decision support, never mortgage advice.
              </p>
              <p className="eyebrow">Start with</p>
              <div className="start-grid">
                {startPrompts.map((prompt) => (
                  <button
                    type="button"
                    className={`start-card${prompt.upload ? ' upload' : ''}`}
                    key={prompt.title}
                    onClick={() => void askCopilot(`${prompt.title}. ${prompt.body}`)}
                  >
                    <strong>{prompt.title}</strong>
                    <span>{prompt.body}</span>
                  </button>
                ))}
              </div>
              {isAskingCopilot ? (
                <div className="copilot-loading" role="status">
                  Asking HomeScout<span aria-hidden="true">…</span>
                </div>
              ) : null}
              {copilotNotice ? (
                <div className="copilot-notice" role="status">{copilotNotice}</div>
              ) : null}
              {copilotAnswer ? (
                <CopilotAnswerCard question={copilotQuestion} answer={copilotAnswer} />
              ) : null}
              <form
                className="composer"
                onSubmit={(event) => {
                  event.preventDefault();
                  const input = event.currentTarget.elements.namedItem('copilot-message') as HTMLInputElement;
                  void askCopilot(input.value);
                  input.value = '';
                }}
              >
                <input
                  name="copilot-message"
                  type="text"
                  aria-label="Ask HomeScout"
                  placeholder="Ask about an area, add a property, or estimate cost…"
                />
                <button type="submit" aria-label="Send">→</button>
              </form>
              <p className="caveat-inline">
                Estimates and public-data summaries — <strong>not mortgage advice</strong>. Speak to a qualified
                adviser before deciding.
              </p>
            </section>
          ) : null}
        </main>

        {(!isMobile || mainTab === 'comparison') ? (
          <aside className="evidence-rail" aria-label="Evidence and estimator">
            <div className="rail-tabs" role="tablist" aria-label="Right panel views">
              <button type="button" role="tab" aria-selected={rightTab === 'evidence'} onClick={() => setRightTab('evidence')}>
                Evidence
              </button>
              <button type="button" role="tab" aria-selected={rightTab === 'estimator'} onClick={() => setRightTab('estimator')}>
                Estimator
              </button>
            </div>

            {rightTab === 'evidence' ? (
              <EvidencePanel evidence={copilotAnswer?.evidence ?? []} />
            ) : (
              <EstimatorPanel
                request={request}
                setRequest={setRequest}
                depositPercent={depositPercent}
                estimate={estimate}
                estimateError={estimateError}
                baseRate={baseRate}
              />
            )}
          </aside>
        ) : null}
      </div>
    </div>
  );
}

// The agent ends its prose with the not-mortgage-advice caveat, which we also render as the
// structured callout below — strip it from the prose so it isn't shown twice (the callout is
// the guaranteed, prominent copy).
function stripTrailingCaveat(text: string): string {
  const lines = text.replace(/\r\n/g, '\n').split('\n');
  while (lines.length && lines[lines.length - 1].trim() === '') lines.pop();
  if (lines.length && /not mortgage advice/i.test(lines[lines.length - 1])) lines.pop();
  return lines.join('\n');
}

function UserIcon() {
  return (
    <svg viewBox="0 0 24 24" width="15" height="15" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <circle cx="12" cy="8" r="3.5" />
      <path d="M5 20c0-3.6 3.1-5.5 7-5.5s7 1.9 7 5.5" />
    </svg>
  );
}

function BotIcon() {
  return (
    <svg viewBox="0 0 24 24" width="15" height="15" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <rect x="4" y="8" width="16" height="11" rx="3.5" />
      <path d="M12 8V4.5" />
      <circle cx="12" cy="3" r="1" fill="currentColor" stroke="none" />
      <circle cx="9.5" cy="13.5" r="1.1" fill="currentColor" stroke="none" />
      <circle cx="14.5" cy="13.5" r="1.1" fill="currentColor" stroke="none" />
    </svg>
  );
}

function CopilotAnswerCard({ question, answer }: { question: string | null; answer: CopilotAnswer }) {
  return (
    <article className="answer-card" aria-label="Copilot answer">
      {question ? (
        <div className="chat-turn user">
          <span className="turn-avatar user" aria-hidden="true"><UserIcon /></span>
          <p className="turn-question">{question}</p>
        </div>
      ) : null}
      <div className="chat-turn bot">
        <span className="turn-avatar bot" aria-label="HomeScout"><BotIcon /></span>
        <div className="turn-body">
          <div className="answer-markdown">{renderMarkdownBlocks(stripTrailingCaveat(answer.text))}</div>
          {answer.toolCalls.length ? (
            <div className="tool-chip-row" aria-label="Tools used">
              {answer.toolCalls.map((tool) => (
                <span className="tool-chip" key={`${tool.name}-${tool.summary}`}>
                  <strong>{tool.name}</strong>
                  {tool.summary}
                </span>
              ))}
            </div>
          ) : null}
          {answer.assumptions.length ? (
            <section className="answer-list" aria-label="Copilot assumptions">
              <h2>Assumptions</h2>
              <ul>
                {answer.assumptions.map((assumption) => (
                  <li key={assumption}>{assumption}</li>
                ))}
              </ul>
            </section>
          ) : null}
          {answer.caveats.length ? (
            <div className="answer-caveats" role="note">
              {answer.caveats.map((caveat) => (
                <p key={caveat}>{caveat}</p>
              ))}
            </div>
          ) : null}
        </div>
      </div>
    </article>
  );
}

function EvidencePanel({ evidence }: { evidence: EvidenceItem[] }) {
  if (!evidence.length) {
    return (
      <div className="evidence-empty" role="note">
        <span className="evidence-mark" aria-hidden="true">▤</span>
        <strong>Evidence appears here</strong>
        <p>Ask a question and HomeScout will show every fact, estimate, assumption and gap — each with its source and freshness.</p>
      </div>
    );
  }

  return (
    <section className="evidence-panel" aria-label="Evidence trail">
      <header className="panel-heading">
        <h2>Evidence trail</h2>
        <p>Every figure from the copilot answer keeps its status, provenance and source.</p>
      </header>
      <div className="evidence-list">
        {evidence.map((item) => (
          <article className="evidence-item" key={`${item.label}-${item.value}-${item.source}`}>
            <div className="evidence-item-head">
              <span className={`kind-chip ${item.kind}`}>{item.kind}</span>
              <span className={`provenance ${item.provenance?.toLowerCase() ?? 'missing'}`}>
                {item.provenance ?? 'Missing'}
              </span>
            </div>
            <strong>{item.value}</strong>
            <span>{item.label}</span>
            <small>{item.source}</small>
          </article>
        ))}
      </div>
    </section>
  );
}

function EstimatorPanel(props: {
  request: MortgageEstimateRequest;
  setRequest: React.Dispatch<React.SetStateAction<MortgageEstimateRequest>>;
  depositPercent: number;
  estimate: MortgageEstimateResult | null;
  estimateError: string | null;
  baseRate: BaseRate | null;
}) {
  const { request, setRequest, depositPercent, estimate, estimateError, baseRate } = props;

  const update = (key: keyof MortgageEstimateRequest, value: string | number) =>
    setRequest((current) => ({
      ...current,
      [key]: key === 'repaymentType' ? value : Number(value),
    }));

  const caveats = estimate?.caveats.length
    ? estimate.caveats
    : ['This is an estimate, not mortgage advice — speak to a qualified adviser before deciding.'];

  return (
    <section className="estimator-panel" aria-label="Mortgage cost estimator">
      <header className="panel-heading">
        <h2>Mortgage cost estimator</h2>
        <p>Uses <strong>your own rate</strong>, not a recommended product. SE10 · Greenwich.</p>
      </header>

      <div className="panel-fields">
        <RangeField label="Property price" value={currency0(request.propertyPrice)} min={150_000} max={900_000} step={5_000} raw={request.propertyPrice} onChange={(v) => update('propertyPrice', v)} />
        <RangeField label="Deposit" value={`${currency0(request.deposit)} · ${depositPercent}%`} min={0} max={Math.max(request.propertyPrice, 1)} step={2_500} raw={request.deposit} onChange={(v) => update('deposit', v)} />
        <RangeField label="Interest rate (your figure)" value={`${request.annualInterestRatePercent}%`} min={1} max={10} step={0.1} raw={request.annualInterestRatePercent} onChange={(v) => update('annualInterestRatePercent', v)} />
        <RangeField label="Term" value={`${request.termYears} yrs`} min={5} max={40} step={1} raw={request.termYears} onChange={(v) => update('termYears', v)} />
        <fieldset className="segmented-control">
          <legend>Repayment type</legend>
          <div className="segmented-options">
            <button type="button" aria-pressed={request.repaymentType === 'Repayment'} onClick={() => update('repaymentType', 'Repayment')}>Repayment</button>
            <button type="button" aria-pressed={request.repaymentType === 'InterestOnly'} onClick={() => update('repaymentType', 'InterestOnly')}>Interest-only</button>
          </div>
        </fieldset>
      </div>

      {estimateError ? (
        <div className="empty-state" role="status">
          The mortgage API could not be reached from this preview. Connect the HomeScout API service to calculate this estimate.
        </div>
      ) : (
        <>
          <div className="payment-card">
            <span>Monthly payment<span className="kind-chip estimate">estimate</span></span>
            <strong>{estimate === null ? 'Loading…' : currency2(estimate.monthlyPayment)}</strong>
          </div>
          <dl className="metric-rows">
            <MetricRow label="Loan amount" value={currency0(estimate?.loan)} />
            <MetricRow label="Loan-to-value" value={estimate === null ? 'Missing' : `${pct1.format(estimate.ltvPercent)}%`} />
            <MetricRow label="Total interest" value={currency0(estimate?.totalInterest)} />
            <MetricRow label="Total repayable" value={estimate?.totalRepayment == null ? '—' : currency0(estimate.totalRepayment)} />
          </dl>
          <div className="stress-row">
            <span>+3% stress payment</span>
            <strong>{currency0(estimate?.stressTest.monthlyPayment)}</strong>
          </div>
        </>
      )}

      <div className="base-rate-line">
        <span className={`provenance ${baseRate?.provenance.toLowerCase() ?? 'missing'}`}>{baseRate?.provenance ?? 'Missing'}</span>
        {baseRate === null ? 'BoE base rate unavailable' : `BoE base rate ${baseRate.ratePercent}% — context only`}
      </div>

      <section className="assumption-block" aria-labelledby="assumptions-heading">
        <h3 id="assumptions-heading">Assumptions</h3>
        <ul>
          {(estimate?.assumptions ?? ['Mortgage calculation comes from /api/mortgage/estimate.']).map((assumption) => (
            <li key={assumption}>{assumption}</li>
          ))}
        </ul>
      </section>

      <div className="caveat">
        {caveats.map((caveat) => (
          <p key={caveat}>{caveat}</p>
        ))}
      </div>
    </section>
  );
}

function RangeField(props: {
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

function MetricRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="metric-row">
      <dt>{label}</dt>
      <dd>{value}</dd>
    </div>
  );
}

export default App;
