/** Visible movie rows in the expanded long frame. Easy to change later. */
export const LIST_VISIBLE_ROWS = 8;

export type WatchlistHeadline = {
  title: string;
  subtitle: string | null;
};

const YEAR_RANGE = /(\d{4})\s*[–—−-]\s*(\d{4})/;

const formatYearRange = (from: string, to: string): string => `${from} – ${to}`;

/**
 * Pull a year-range subtitle out of a watchlist name when the source title includes one
 * (newline, trailing "2000 – 2012", or ": 2000-2012"). Otherwise the full name is the title.
 */
export const splitWatchlistHeadline = (name: string): WatchlistHeadline => {
  const trimmed = name.trim();
  if (!trimmed) {
    return { title: "", subtitle: null };
  }

  const lines = trimmed
    .split(/\r?\n/)
    .map((line) => line.trim())
    .filter(Boolean);
  if (lines.length >= 2) {
    return { title: lines[0], subtitle: lines.slice(1).join(" ") };
  }

  const colonYear = trimmed.match(new RegExp(`^(.*?):\\s*${YEAR_RANGE.source}$`));
  if (colonYear?.[1] && colonYear[2] && colonYear[3]) {
    return {
      title: `${colonYear[1].trim()}:`,
      subtitle: formatYearRange(colonYear[2], colonYear[3]),
    };
  }

  const trailingYear = trimmed.match(new RegExp(`^(.*?)\\s+${YEAR_RANGE.source}$`));
  if (trailingYear?.[1]?.trim() && trailingYear[2] && trailingYear[3]) {
    return {
      title: trailingYear[1].trim(),
      subtitle: formatYearRange(trailingYear[2], trailingYear[3]),
    };
  }

  return { title: trimmed, subtitle: null };
};
