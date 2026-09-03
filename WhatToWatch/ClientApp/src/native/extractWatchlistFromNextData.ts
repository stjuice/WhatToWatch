import type { ExtractedWatchlistPage, ExtractedMoviePage } from "./imdbImporter";

export type ExtractOptions = {
  pathname?: string;
  documentTitle?: string;
  currentUrl?: string;
};

export function extractWatchlistFromNextData(
  data: unknown,
  options: ExtractOptions = {}
): ExtractedWatchlistPage | null {
  const movies: ExtractedMoviePage[] = [];
  const seen = new Set<string>();

  visit(data, movies, seen);

  if (movies.length === 0) {
    return null;
  }

  const listId = extractListId(options.pathname);
  const title = findListName(data) ?? options.documentTitle ?? listId ?? "IMDb list";
  const hasNextPage = readHasNextPage(data);

  return {
    listId: listId ?? title,
    title,
    movies,
    hasNextPage,
    nextPageUrl: hasNextPage ? buildNextPageUrl(options.currentUrl) : null,
  };
}

function extractListId(pathname: string | undefined): string | null {
  const list = pathname?.match(/\/list\/(ls\d+)/i);
  if (list?.[1]) return list[1];

  const watchlist = pathname?.match(
    /\/user\/((?:ur\d+|p\.[a-z0-9]+))\/watchlist/i
  );
  if (watchlist?.[1]) return watchlist[1];

  const chart = pathname?.match(/\/chart\/([a-z0-9][a-z0-9-]*)/i);
  return chart?.[1] ? `chart-${chart[1].toLowerCase()}` : null;
}

function buildNextPageUrl(currentUrl: string | undefined): string | null {
  if (!currentUrl) return null;

  try {
    const url = new URL(currentUrl);
    if (url.protocol !== "https:" || !/(^|\.)imdb\.com$/i.test(url.hostname)) {
      return null;
    }

    const current = Number.parseInt(url.searchParams.get("page") ?? "1", 10);
    url.searchParams.set(
      "page",
      String(Number.isFinite(current) && current > 0 ? current + 1 : 2)
    );
    return url.toString();
  } catch {
    return null;
  }
}

function readHasNextPage(node: unknown): boolean {
  if (node == null || typeof node !== "object") return false;

  if (Array.isArray(node)) {
    return node.some((item) => readHasNextPage(item));
  }

  const record = node as Record<string, unknown>;

  if (
    record.titleListItemSearch &&
    typeof record.titleListItemSearch === "object" &&
    record.titleListItemSearch !== null
  ) {
    const search = record.titleListItemSearch as Record<string, unknown>;
    const pageInfo = search.pageInfo;
    if (pageInfo && typeof pageInfo === "object" && pageInfo !== null) {
      return (pageInfo as { hasNextPage?: unknown }).hasNextPage === true;
    }
  }

  return Object.values(record).some((value) => readHasNextPage(value));
}

function visit(node: unknown, movies: ExtractedMoviePage[], seen: Set<string>): void {
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
      titleType: readTitleType(record),
    });
  }

  for (const value of Object.values(record)) {
    visit(value, movies, seen);
  }
}

function readTitleType(record: Record<string, unknown>): string | null {
  const titleType = record.titleType;
  if (!titleType || typeof titleType !== "object") {
    return null;
  }

  const id = (titleType as { id?: unknown }).id;
  return typeof id === "string" && id ? id : null;
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
