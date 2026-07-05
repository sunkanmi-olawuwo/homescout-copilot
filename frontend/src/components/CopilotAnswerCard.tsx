import type { CopilotAnswer } from '../types';
import { renderMarkdownBlocks, splitTrailingCaveat } from '../lib/markdown';
import { BotIcon, UserIcon } from './icons';

// One copilot exchange: the user's question turn + the bot's answer (sanitized markdown, tool chips,
// assumptions, and the caveat callout). A trailing "not mortgage advice" prose line is promoted into
// the callout when the structured caveats array is empty (shown once, prominently).
export function CopilotAnswerCard({ question, answer }: { question: string | null; answer: CopilotAnswer }) {
  const { markdown, trailingCaveat } = splitTrailingCaveat(answer.text);
  const caveats = answer.caveats.length ? answer.caveats : trailingCaveat ? [trailingCaveat] : [];

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
          <div className="answer-markdown">{renderMarkdownBlocks(markdown)}</div>
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
          {caveats.length ? (
            <div className="answer-caveats" role="note">
              {caveats.map((caveat) => (
                <p key={caveat}>{caveat}</p>
              ))}
            </div>
          ) : null}
        </div>
      </div>
    </article>
  );
}
