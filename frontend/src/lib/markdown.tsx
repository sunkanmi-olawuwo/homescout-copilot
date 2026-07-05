import type { ReactNode } from 'react';

// A deliberately small, safe Markdown subset: headings (# ##), bullet/numbered lists, bold, emphasis,
// inline code, and links restricted to safe schemes. Output is React nodes (no dangerouslySetInnerHTML),
// so raw HTML is escaped by React and only whitelisted markup is produced.

export function isSafeLinkUrl(url: string) {
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

// A single inline token: the rendered node and where scanning resumes after it.
type InlineToken = { node: ReactNode; nextCursor: number };
type InlineMatcher = (source: string, at: number, key: string) => InlineToken | null;

// Each matcher recognises one delimiter at `at` and returns null if it doesn't apply, keeping the
// main scan loop flat. A delimited span only matches when its closing delimiter is found.
const matchCode: InlineMatcher = (source, at, key) => {
  if (!source.startsWith('`', at)) return null;
  const end = source.indexOf('`', at + 1);
  if (end <= at) return null;
  return { node: <code key={key}>{source.slice(at + 1, end)}</code>, nextCursor: end + 1 };
};

const matchStrong: InlineMatcher = (source, at, key) => {
  if (!source.startsWith('**', at)) return null;
  const end = source.indexOf('**', at + 2);
  if (end <= at) return null;
  return { node: <strong key={key}>{source.slice(at + 2, end)}</strong>, nextCursor: end + 2 };
};

const matchEmphasis: InlineMatcher = (source, at, key) => {
  if (!source.startsWith('*', at) || source.startsWith('**', at)) return null;
  const end = source.indexOf('*', at + 1);
  if (end <= at) return null;
  return { node: <em key={key}>{source.slice(at + 1, end)}</em>, nextCursor: end + 1 };
};

const matchLink: InlineMatcher = (source, at, key) => {
  if (!source.startsWith('[', at)) return null;
  const labelEnd = source.indexOf(']', at + 1);
  const urlStart = labelEnd >= 0 ? source.indexOf('(', labelEnd + 1) : -1;
  const urlEnd = urlStart >= 0 ? source.indexOf(')', urlStart + 1) : -1;
  if (labelEnd <= at || urlStart !== labelEnd + 1 || urlEnd <= urlStart) return null;

  const label = source.slice(at + 1, labelEnd);
  const url = source.slice(urlStart + 1, urlEnd);
  // Unsafe schemes render as plain text, never as a link.
  const node = isSafeLinkUrl(url) ? (
    <a key={key} href={url.trim()} target="_blank" rel="noreferrer noopener">
      {label}
    </a>
  ) : (
    <span key={key}>{label}</span>
  );
  return { node, nextCursor: urlEnd + 1 };
};

const INLINE_MATCHERS: InlineMatcher[] = [matchCode, matchStrong, matchLink, matchEmphasis];

export function renderInlineMarkdown(source: string, keyPrefix: string): ReactNode[] {
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

    const token = firstInlineMatch(source, next, `${keyPrefix}-${key++}`);
    if (token) {
      nodes.push(token.node);
      cursor = token.nextCursor;
      continue;
    }

    // No delimiter matched here (e.g. an unclosed span): emit the literal char and move on.
    pushText(nodes, source[next], `${keyPrefix}-literal-${key++}`);
    cursor = next + 1;
  }

  return nodes;
}

function firstInlineMatch(source: string, at: number, key: string): InlineToken | null {
  for (const matcher of INLINE_MATCHERS) {
    const token = matcher(source, at, key);
    if (token) return token;
  }
  return null;
}

export function renderMarkdownBlocks(source: string) {
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

// The agent ends its prose with the not-mortgage-advice caveat, which is also rendered as the
// structured callout — split it out of the prose so it isn't shown twice (the callout is the
// guaranteed, prominent copy).
export function splitTrailingCaveat(text: string): { markdown: string; trailingCaveat: string | null } {
  const lines = text.replace(/\r\n/g, '\n').split('\n');
  while (lines.length && lines[lines.length - 1].trim() === '') lines.pop();
  const lastLine = lines[lines.length - 1]?.trim() ?? '';
  const trailingCaveat = /not mortgage advice/i.test(lastLine) ? lastLine : null;
  if (trailingCaveat) lines.pop();
  return { markdown: lines.join('\n'), trailingCaveat };
}
