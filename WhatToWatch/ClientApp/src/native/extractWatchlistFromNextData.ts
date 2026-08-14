import type { ImportedMovie, ImportedWatchlist } from "./imdbImporter";

export type ExtractOptions = {
  pathname?: string;
  documentTitle?: string;
};

/**
 * Normalizes IMDb `__NEXT_DATA__` into the app watchlist shape.
 *
 * Walks the whole tree (same strategy as ImdbListHtmlParser) instead of relying on a fixed path,
 * because IMDb nests list items differently across list / chart / watchlist renderers.
 *
 * Kept in TypeScript so it can be unit-tested. The WebView loads the matching IIFE from
 * `android/app/src/main/assets/imdb/extractWatchlist.js` — keep those two in sync
 * (the Vitest suite asserts they agree on the same fixtures).
 */
export function extractWatchlistFromNextData(
  data: unknown,
  options: ExtractOptions = {}
): ImportedWatchlist | null {
  const movies: ImportedMovie[] = [];
  const seen = new Set<string>();

  visit(data, movies, seen);

  if (movies.length === 0) {
    return null;
  }

  const listId = options.pathname?.match(/(ls\d+)/)?.[1] ?? null;
  const title = findListName(data) ?? options.documentTitle ?? listId ?? "IMDb list";

  return {
    listId: listId ?? title,
    title,
    movies,
  };
}

function visit(node: unknown, movies: ImportedMovie[], seen: Set<string>): void {
  if (node == null || typeof node !== "object") {
    return;
  }

  if (Array.isArray(node)) {
    for (const item of node) {
      visit(item, movies, seen);
    }
    return;
  }

  const record = node as Record<string, unknown>;
  const id = record.id;
  const titleText =
    record.titleText &&
    typeof record.titleText === "object" &&
    record.titleText !== null &&
    typeof (record.titleText as { text?: unknown }).text === "string"
      ? ((record.titleText as { text: string }).text)
      : null;

  if (typeof id === "string" && id.startsWith("tt") && titleText && !seen.has(id)) {
    seen.add(id);

    const releaseYear =
      record.releaseYear && typeof record.releaseYear === "object" && record.releaseYear !== null
        ? (record.releaseYear as { year?: unknown }).year
        : null;
    const primaryImage =
      record.primaryImage && typeof record.primaryImage === "object" && record.primaryImage !== null
        ? (record.primaryImage as { url?: unknown }).url
        : null;

    movies.push({
      imdbId: id,
      title: titleText,
      year: typeof releaseYear === "number" ? releaseYear : null,
      imageUrl: typeof primaryImage === "string" ? primaryImage : null,
    });
  }

  for (const value of Object.values(record)) {
    visit(value, movies, seen);
  }
}

function findListName(node: unknown): string | null {
  if (node == null || typeof node !== "object") {
    return null;
  }

  if (Array.isArray(node)) {
    for (const item of node) {
      const name = findListName(item);
      if (name) {
        return name;
      }
    }
    return null;
  }

  const record = node as Record<string, unknown>;

  for (const key of ["list", "predefinedList"] as const) {
    const list = record[key];
    if (list && typeof list === "object" && !Array.isArray(list)) {
      const listRecord = list as Record<string, unknown>;
      const nameObj = listRecord.name;
      if (nameObj && typeof nameObj === "object" && nameObj !== null) {
        const originalText = (nameObj as { originalText?: unknown }).originalText;
        const text = (nameObj as { text?: unknown }).text;
        if (typeof originalText === "string" && originalText) {
          return originalText;
        }
        if (typeof text === "string" && text) {
          return text;
        }
      }

      const nameText = listRecord.nameText;
      if (nameText && typeof nameText === "object" && nameText !== null) {
        const text = (nameText as { text?: unknown }).text;
        if (typeof text === "string" && text) {
          return text;
        }
      }
    }
  }

  for (const value of Object.values(record)) {
    const name = findListName(value);
    if (name) {
      return name;
    }
  }

  return null;
}
