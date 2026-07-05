import type { SavedComparison } from '../data';
import type { ConversationSummary } from '../types';
import { HistoryList } from './HistoryList';

// The left sidebar: new-comparison action, saved-search filter, the saved-comparisons list, the
// signed-in user's conversation history, and the workspace nav footer. Collapses into a drawer on
// mobile (driven by navOpen).
export function LeftRail(props: {
  isMobile: boolean;
  navOpen: boolean;
  savedComparisons: SavedComparison[];
  showHistory: boolean;
  history: ConversationSummary[];
  onResume: (sessionId: string) => void;
}) {
  const { isMobile, navOpen, savedComparisons, showHistory, history, onResume } = props;

  return (
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
      {showHistory ? <HistoryList conversations={history} onResume={onResume} /> : null}
      <nav className="rail-footer" aria-label="Workspace">
        <button type="button">Case file<span className="badge">2</span></button>
        <button type="button">Preferences<span className="badge">5</span></button>
        <button type="button">Settings</button>
      </nav>
    </aside>
  );
}
