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
