import type { ConversationSummary } from '../types';

// The signed-in user's past conversations (left rail). Clicking an item asks the app to resume it
// (re-open on the server) so the next question continues that thread.
export function HistoryList(props: {
  conversations: ConversationSummary[];
  onResume: (sessionId: string) => void;
}) {
  const { conversations, onResume } = props;

  return (
    <section aria-labelledby="your-conversations-heading">
      <h2 id="your-conversations-heading">Your conversations</h2>
      {conversations.length ? (
        <div className="saved-list">
          {conversations.map((conversation) => (
            <button
              type="button"
              className="saved-item"
              key={conversation.sessionId}
              onClick={() => onResume(conversation.sessionId)}
            >
              <strong>Conversation</strong>
              <span>{new Date(conversation.lastActiveAt).toLocaleDateString('en-GB')}</span>
              <small>Tap to continue</small>
            </button>
          ))}
        </div>
      ) : (
        <p className="history-empty">Your conversations appear here once you chat while signed in.</p>
      )}
    </section>
  );
}
