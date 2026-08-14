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
      rating: readRating(record),
      plot: readPlot(record),
      runtimeMinutes: readRuntimeMinutes(record),
      director: readDirector(record),
      genres: readGenres(record),
    });
  }

  for (const value of Object.values(record)) {
    visit(value, movies, seen);
  }
}

function readRating(record: Record<string, unknown>): number | null {
  const summary = record.ratingsSummary;
  if (!summary || typeof summary !== "object") {
    return null;
  }

  const rating = (summary as { aggregateRating?: unknown }).aggregateRating;
  return typeof rating === "number" ? rating : null;
}

function readPlot(record: Record<string, unknown>): string | null {
  const plot = record.plot;
  if (!plot || typeof plot !== "object") {
    return null;
  }

  const plotText = (plot as { plotText?: { plainText?: unknown } }).plotText;
  return typeof plotText?.plainText === "string" ? plotText.plainText : null;
}

function readRuntimeMinutes(record: Record<string, unknown>): number | null {
  const runtime = record.runtime;
  if (!runtime || typeof runtime !== "object") {
    return null;
  }

  const value = runtime as { seconds?: unknown; minutes?: unknown };
  if (typeof value.seconds === "number" && value.seconds > 0) {
    return Math.floor(value.seconds / 60);
  }

  if (typeof value.minutes === "number" && value.minutes > 0) {
    return value.minutes;
  }

  return null;
}

function readGenres(record: Record<string, unknown>): string[] {
  const titleGenres = record.titleGenres;
  if (!titleGenres || typeof titleGenres !== "object") {
    return [];
  }

  const genres = (titleGenres as { genres?: unknown }).genres;
  if (!Array.isArray(genres)) {
    return [];
  }

  const result: string[] = [];
  for (const entry of genres) {
    if (!entry || typeof entry !== "object") {
      continue;
    }

    const genre = (entry as { genre?: { text?: unknown } }).genre;
    if (typeof genre?.text === "string" && genre.text) {
      result.push(genre.text);
    }
  }

  return result;
}

function readDirector(record: Record<string, unknown>): string | null {
  const fromV2 = readDirectorFromCredits(
    record.principalCreditsV2,
    (entry) => {
      const text = (entry.grouping as { text?: unknown } | undefined)?.text;
      return typeof text === "string" && /^directors?$/i.test(text);
    }
  );
  if (fromV2) {
    return fromV2;
  }

  return readDirectorFromCredits(record.principalCredits, (entry) => {
    const category = entry.category as { id?: unknown; text?: unknown } | undefined;
    const id = category?.id;
    const text = category?.text;
    return (
      (typeof id === "string" && id.toLowerCase() === "director") ||
      (typeof text === "string" && /^directors?$/i.test(text))
    );
  });
}

function readDirectorFromCredits(
  credits: unknown,
  isDirector: (entry: Record<string, unknown>) => boolean
): string | null {
  if (!Array.isArray(credits)) {
    return null;
  }

  for (const entry of credits) {
    if (!entry || typeof entry !== "object") {
      continue;
    }

    const record = entry as Record<string, unknown>;
    if (!isDirector(record)) {
      continue;
    }

    const name = readFirstCreditName(record);
    if (name) {
      return name;
    }
  }

  return null;
}

function readFirstCreditName(entry: Record<string, unknown>): string | null {
  const credits = entry.credits;
  if (Array.isArray(credits)) {
    for (const credit of credits) {
      const name = readCreditName(credit);
      if (name) {
        return name;
      }
    }
  }

  return readCreditName(entry);
}

function readCreditName(credit: unknown): string | null {
  if (!credit || typeof credit !== "object") {
    return null;
  }

  const name = (credit as { name?: { nameText?: { text?: unknown } } }).name;
  const text = name?.nameText?.text;
  return typeof text === "string" && text ? text : null;
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
