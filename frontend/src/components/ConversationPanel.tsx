import type { StartPrompt } from '../data';
import type { CopilotAnswer } from '../types';
import { CopilotAnswerCard } from './CopilotAnswerCard';
import { NewChatIcon } from './icons';

// The copilot conversation surface: status header (+ New conversation once active), the empty-state
// hero + start prompts, live/loading + notice states, the answer card, the composer, and the inline
// caveat. All state lives in App; this renders it and raises intent via onAsk / onResetConversation.
export function ConversationPanel(props: {
  conversationActive: boolean;
  isResettingConversation: boolean;
  onResetConversation: () => void;
  startPrompts: StartPrompt[];
  onAsk: (message: string) => void;
  isAskingCopilot: boolean;
  copilotNotice: string | null;
  copilotAnswer: CopilotAnswer | null;
  copilotQuestion: string | null;
}) {
  const {
    conversationActive,
    isResettingConversation,
    onResetConversation,
    startPrompts,
    onAsk,
    isAskingCopilot,
    copilotNotice,
    copilotAnswer,
    copilotQuestion,
  } = props;

  return (
    <section className={`conversation${conversationActive ? ' active' : ''}`} aria-label="HomeScout copilot">
      <div className="conversation-header">
        <span className="status-pill"><i aria-hidden="true" />Copilot ready · public-data tools connected</span>
        {conversationActive ? (
          <button
            className="new-thread-button"
            type="button"
            aria-label="New conversation"
            disabled={isResettingConversation}
            onClick={onResetConversation}
          >
            <NewChatIcon />
            <span className="new-thread-label">
              {isResettingConversation ? 'Starting…' : 'New conversation'}
            </span>
          </button>
        ) : null}
      </div>
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
            onClick={() => onAsk(`${prompt.title}. ${prompt.body}`)}
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
          onAsk(input.value);
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
  );
}
